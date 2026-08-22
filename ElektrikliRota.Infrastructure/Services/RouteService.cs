using System.Text.Json;
using ElektrikliRota.Core.Entities;
using ElektrikliRota.Core.Interfaces;
using ElektrikliRota.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace ElektrikliRota.Infrastructure.Services;

public class RouteService : IRouteService
{
    private readonly IStationRepository _stationRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RouteService>? _logger;
    private readonly string _osrmBaseUrl;

    private const double RealisticRangeFactor  = 0.90;  // Fabrika menzilinin %90'ı
    private const double ChargeThresholdFactor  = 0.10;  // Bu miktarı kalınca şarj et
    private const double NearbyCorridorKm       = 3.0;  // Rota koridoru genişliği (km)
    private const string BrowserUserAgent      = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";

    public RouteService(
        IStationRepository stationRepository, 
        IHttpClientFactory httpClientFactory, 
        IConfiguration configuration,
        ILogger<RouteService>? logger = null)
    {
        _stationRepository = stationRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        
        var configuredUrl = configuration["OsrmSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            _osrmBaseUrl = "https://router.project-osrm.org";
        }
        else
        {
            // OSRM için https tercih et
            _osrmBaseUrl = configuredUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) 
                ? "https://" + configuredUrl[7..] 
                : configuredUrl;
        }
    }

    public async Task<RouteResult> CalculateRouteAsync(
        Location start, Location end, Vehicle vehicle,
        List<string> preferredBrands, List<string> connectorTypes, int initialCharge, int additionalConsumptionPercent)
    {
        // 1. İlk OSRM çağrısı — alternatives=true, ham geometri
        var initialRoutes = await FetchOsrmRoutes(start, end, null);
        if (initialRoutes.Count == 0)
        {
            _logger?.LogError("OSRM'den rota verisi alınamadı. Başlangıç: ({StartLat}, {StartLng}) -> Bitiş: ({EndLat}, {EndLng})",
                start.Latitude, start.Longitude, end.Latitude, end.Longitude);
            throw new Exception("Harita ve rota servisinden (OSRM) yol verisi alınamadı. Lütfen daha sonra tekrar deneyiniz.");
        }

        // 2. İstasyon havuzu filtrele
        var allStations = (await _stationRepository.GetAllStationsAsync()).ToList();

        var filtered = preferredBrands.Count > 0
            ? allStations.Where(s => preferredBrands.Contains(s.Brand, StringComparer.OrdinalIgnoreCase)).ToList()
            : allStations;

        if (connectorTypes.Count > 0)
        {
            var wantsAc  = connectorTypes.Any(c => c.Equals("AC",  StringComparison.OrdinalIgnoreCase));
            var wantsDc  = connectorTypes.Any(c => c.Equals("DC",  StringComparison.OrdinalIgnoreCase));
            var wantsHpc = connectorTypes.Any(c => c.Equals("HPC", StringComparison.OrdinalIgnoreCase));

            filtered = filtered.Where(s =>
                (wantsAc  && s.AcConnectorCount  > 0) ||
                (wantsDc  && s.DcConnectorCount  > 0) ||
                (wantsHpc && s.HpcConnectorCount > 0)
            ).ToList();
        }

        // 3. Her OSRM alternatifi için şarj planı + waypoint rotası + yakın istasyonlar
        var alternatives = new List<RouteAlternative>();

        for (int idx = 0; idx < initialRoutes.Count; idx++)
        {
            var (initialPath, distKm, durSec) = initialRoutes[idx];

            // a) Şarj duraklarını bul + varış şarj yüzdesini hesapla
            var (stops, arrivalCharge) = BuildChargeStops(initialPath, distKm, durSec, vehicle, filtered.ToList(), initialCharge, additionalConsumptionPercent);

            if (stops == null)
            {
                // Bu alternatif rota için menzil içinde uygun şarj istasyonu bulunamadı
                continue;
            }

            // b) Şarj durakları varsa OSRM'i waypoint'lerle yeniden çağır
            //    → rota artık gerçekten o noktalardan geçer
            List<Location> finalPath = initialPath;
            double finalDist = distKm;
            double finalDur  = durSec;

            if (stops.Count > 0)
            {
                var waypoints = stops.Select(s => new Location
                {
                    Latitude  = s.Latitude,
                    Longitude = s.Longitude,
                }).ToList();

                var waypointRoutes = await FetchOsrmRoutes(start, end, waypoints);
                if (waypointRoutes.Count > 0)
                    (finalPath, finalDist, finalDur) = waypointRoutes[0];
            }

            // c) Rota koridoru boyunca tüm yakın istasyonlar (bilgi pinleri)
            var nearbyStations = GetNearbyStations(finalPath, allStations);

            // d) Şarj bekleme süresi (HPC: 25dk, DC: 40dk, AC: 90dk)
            var chargeTimeHours = EstimateChargeTime(stops);

            // e) Tahmini şarj maliyeti
            var estimatedCost = EstimateChargeCost(stops, vehicle);

            alternatives.Add(new RouteAlternative
            {
                Index                  = idx,
                Path                   = finalPath,
                Stops                  = stops,
                NearbyStations         = nearbyStations,
                TotalDistanceKm        = Math.Round(finalDist, 2),
                EstimatedDurationHours = Math.Round(finalDur / 3600.0, 2),
                ChargeTimeHours        = chargeTimeHours,
                ArrivalChargePercentage = arrivalCharge,
                EstimatedCost          = estimatedCost
            });
        }

        if (alternatives.Count == 0)
        {
            throw new Exception("Seçtiğiniz filtrelerle bu rotada ulaşılabilecek uygun bir şarj istasyonu bulunamadı. Lütfen filtrelerinizi (İstasyon markası, Şarj tipi vs.) esneterek tekrar deneyin.");
        }

        var best = alternatives[0];
        return new RouteResult
        {
            Path                   = best.Path,
            Stops                  = best.Stops,
            NearbyStations         = best.NearbyStations,
            TotalDistanceKm        = best.TotalDistanceKm,
            EstimatedDurationHours = best.EstimatedDurationHours,
            ChargeTimeHours        = best.ChargeTimeHours,
            ArrivalChargePercentage = best.ArrivalChargePercentage,
            EstimatedCost          = best.EstimatedCost,
            Alternatives           = alternatives,
        };
    }

    // ─── OSRM yardımcı — waypoint'li veya noktasız çağrı ───────────────────
    private async Task<List<(List<Location> Path, double DistKm, double DurSec)>> FetchOsrmRoutes(
        Location start, Location end, List<Location>? waypoints)
    {
        var coordinatesPath = new System.Text.StringBuilder();

        // Başlangıç
        coordinatesPath.Append($"{start.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{start.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        // Ara noktalar
        if (waypoints != null)
        {
            foreach (var wp in waypoints)
            {
                coordinatesPath.Append(';');
                coordinatesPath.Append($"{wp.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{wp.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }
        }

        // Bitiş
        coordinatesPath.Append(';');
        coordinatesPath.Append($"{end.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{end.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        var alts = (waypoints == null || waypoints.Count == 0) ? "&alternatives=true" : "";
        var relativeUrl = $"/route/v1/driving/{coordinatesPath}?overview=full&geometries=geojson{alts}";

        // OSRM Base URL listesi (Birincil ve yedek)
        var targetBases = new List<string> { _osrmBaseUrl.TrimEnd('/') };
        if (_osrmBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            targetBases.Add("http://" + _osrmBaseUrl[8..].TrimEnd('/'));
        }
        else if (_osrmBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            targetBases.Insert(0, "https://" + _osrmBaseUrl[7..].TrimEnd('/'));
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        foreach (var baseUrl in targetBases.Distinct())
        {
            var fullUrl = $"{baseUrl}{relativeUrl}";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                request.Headers.Add("User-Agent", BrowserUserAgent);
                request.Headers.Add("Accept", "application/json");

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning("OSRM ({Url}) HTTP {StatusCode} döndürdü.", fullUrl, (int)response.StatusCode);
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);

                if (!document.RootElement.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                {
                    _logger?.LogWarning("OSRM ({Url}) 'routes' dizisi boş veya bulunamadı.", fullUrl);
                    continue;
                }

                var results = new List<(List<Location>, double, double)>();
                foreach (var osrmRoute in routes.EnumerateArray())
                {
                    var dist   = osrmRoute.GetProperty("distance").GetDouble() / 1000.0;
                    var dur    = osrmRoute.GetProperty("duration").GetDouble();
                    var coords = osrmRoute.GetProperty("geometry").GetProperty("coordinates");

                    var path = new List<Location>();
                    foreach (var coord in coords.EnumerateArray())
                        path.Add(new Location { Longitude = coord[0].GetDouble(), Latitude = coord[1].GetDouble() });

                    results.Add((path, dist, dur));
                }

                if (results.Count > 0)
                {
                    return results;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "OSRM ({Url}) isteğinde hata oluştu.", fullUrl);
            }
        }

        return new();
    }

    // ─── Şarj süresi tahmini ────────────────────────────────────────────────
    // HPC: ~25 dk, DC: ~40 dk, AC: ~90 dk (10%→80% dolum)
    private static double EstimateChargeTime(List<Station> stops)
    {
        double totalMinutes = 0;
        foreach (var s in stops)
        {
            if (s.HpcConnectorCount > 0)      totalMinutes += 25;
            else if (s.DcConnectorCount > 0)  totalMinutes += 40;
            else if (s.AcConnectorCount > 0)  totalMinutes += 90;
            else                              totalMinutes += 40; // bilinmeyen → DC varsayım
        }
        return Math.Round(totalMinutes / 60.0, 2);
    }

    // ─── Tahmini Şarj Maliyeti Hesaplama ────────────────────────────────────
    private static double EstimateChargeCost(List<Station> stops, Vehicle vehicle)
    {
        double totalCost = 0;
        foreach (var stop in stops)
        {
            // BuildChargeStops mantığında araç %80'e kadar doldurulur
            // Yüklenen yüzdelik miktar = 80 - varış yüzdesi
            int chargedPct = 80 - (stop.ArrivalChargePercentage ?? 0);
            if (chargedPct < 0) chargedPct = 0;

            double addedKwh = (chargedPct / 100.0) * vehicle.BatteryCapacityKWh;
            
            double pricePerKwh = PricingConstants.DefaultChargePrice;
            if (!string.IsNullOrEmpty(stop.Brand) && PricingConstants.Prices.TryGetValue(stop.Brand, out var brandPrice))
            {
                pricePerKwh = brandPrice.DC; // Route over fast charge implies DC rate usually
            }

            totalCost += addedKwh * pricePerKwh;
        }
        
        return Math.Round(totalCost, 2);
    }

    // ─── Zorunlu şarj durakları algoritması ─────────────────────────────────
    // Mantık: Mevcut menzil eşiği aşıldığında, kalan menzil içinde rota
    // üzerindeki en yakın GERÇEK istasyonu seçer. Sanal öneri üretmez.
    public (List<Station>? Stops, int ArrivalChargePercentage) BuildChargeStops(
        List<Location> path, double totalDistKm, double totalDurSec, Vehicle vehicle, List<Station> available, int initialCharge, int additionalConsumptionPercent)
    {
        // Dinamik Tüketim Çarpanı (Ortalama Hıza Göre)
        double avgSpeedKmH = totalDistKm / (totalDurSec / 3600.0);
        double consumptionMultiplier = 1.0;
        
        if (avgSpeedKmH > 110)      consumptionMultiplier = 1.25; // %25 fazla tüketim
        else if (avgSpeedKmH > 80) consumptionMultiplier = 1.10; // %10 fazla tüketim
        else if (avgSpeedKmH < 50) consumptionMultiplier = 0.90; // %10 daha az tüketim

        // Çarpana göre gerçek menzili yeniden hesapla (Hızlı gidildiyse menzil düşer)
        double manualConsumptionFactor = 1.0 + (additionalConsumptionPercent / 100.0);
        double realMax      = (vehicle.RangeKm * RealisticRangeFactor) / (consumptionMultiplier * manualConsumptionFactor);
        double threshold    = realMax * ChargeThresholdFactor;
        double currentRange = realMax * (initialCharge / 100.0);
        double covered      = 0;
        var stops           = new List<Station>();
        var pool            = available.ToList();

        for (int i = 1; i < path.Count; i++)
        {
            var seg = CalculateDistance(
                path[i - 1].Latitude, path[i - 1].Longitude,
                path[i].Latitude,     path[i].Longitude);

            covered      += seg;
            currentRange -= seg;

            // Menzil eşiği (%10) aşıldığında her zaman durak ara
            if (currentRange <= threshold)
            {
                Station? best = null;
                double bestScore = double.MaxValue;

                double lookaheadBudget = currentRange;
                for (int j = i; j < path.Count && lookaheadBudget > 0; j++)
                {
                    foreach (var s in pool)
                    {
                        var d = CalculateDistance(path[j].Latitude, path[j].Longitude, s.Latitude, s.Longitude);
                        if (d < 40)
                        {
                            int speedPenalty = 3;
                            if (s.HpcConnectorCount > 0) speedPenalty = 1;
                            else if (s.DcConnectorCount > 0) speedPenalty = 2;

                            double score = d + ((speedPenalty - 1) * 20.0);

                            if (score < bestScore)
                            {
                                bestScore = score;
                                best = s;
                            }
                        }
                    }

                    if (j + 1 < path.Count)
                        lookaheadBudget -= CalculateDistance(
                            path[j].Latitude, path[j].Longitude,
                            path[j + 1].Latitude, path[j + 1].Longitude);
                }

                if (best != null)
                {
                    var stopClone = new Station
                    {
                        Id = best.Id,
                        Brand = best.Brand,
                        Latitude = best.Latitude,
                        Longitude = best.Longitude,
                        Name = best.Name,
                        IsFastCharge = best.IsFastCharge,
                        AcConnectorCount = best.AcConnectorCount,
                        DcConnectorCount = best.DcConnectorCount,
                        HpcConnectorCount = best.HpcConnectorCount,
                        ArrivalChargePercentage = (int)Math.Max(0, Math.Round((currentRange / realMax) * 100))
                    };
                    stops.Add(stopClone);
                    pool.Remove(best);
                    currentRange = realMax * 0.80;
                }
                else
                {
                    // Menzil bitti ancak uygun istasyon bulunamadı (Filtreler yüzünden veya gerçekten yok)
                    return (null, 0);
                }
            }
        }

        // Varış noktasındaki tahmini kalan şarj yüzdesi
        int arrivalPct = (int)Math.Max(0, Math.Min(100, Math.Round((currentRange / realMax) * 100)));
        return (stops, arrivalPct);
    }

    // ─── Rota koridoru boyunca yakın tüm istasyonlar ────────────────────────
    private List<Station> GetNearbyStations(List<Location> path, List<Station> allStations)
    {
        // Performans: rota noktalarını seyrekleştir (her 10. nokta)
        var samplePoints = path
            .Where((_, i) => i % 10 == 0)
            .ToList();

        return allStations
            .Where(station =>
                samplePoints.Any(pt =>
                    CalculateDistance(pt.Latitude, pt.Longitude, station.Latitude, station.Longitude) <= NearbyCorridorKm))
            .ToList();
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = Deg2Rad(lat2 - lat1);
        var dLon = Deg2Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double Deg2Rad(double deg) => deg * (Math.PI / 180);
}
