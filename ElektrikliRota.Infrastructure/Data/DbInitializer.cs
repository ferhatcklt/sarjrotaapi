using System.Text.Json;
using ElektrikliRota.Core.Entities;

namespace ElektrikliRota.Infrastructure.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context, string dataPath)
    {
        if (!context.Vehicles.Any())
            SeedVehicles(context);

        if (!context.Stations.Any())
        {
            var stations = new List<Station>();

            var stationsJsonPath = Path.Combine(dataPath, "stations.json");
            if (File.Exists(stationsJsonPath))
                ParseStationsJson(stationsJsonPath, stations);

            var trugoJsonPath = Path.Combine(dataPath, "trugo_stations.json");
            if (File.Exists(trugoJsonPath))
                ParseTrugoGeoJson(trugoJsonPath, stations);

            var teslaJsonPath = Path.Combine(dataPath, "tesla_stations.json");
            if (File.Exists(teslaJsonPath))
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var teslaData = JsonSerializer.Deserialize<List<Station>>(File.ReadAllText(teslaJsonPath), opts);
                if (teslaData != null) 
                {
                    foreach(var t in teslaData) 
                    {
                        t.MaxPowerKw = 250; // Tesla V3 Superchargers are typically 250 kW
                        stations.Add(t);
                    }
                }
            }

            var esarjJsonPath = Path.Combine(dataPath, "esarj.json");
            if (File.Exists(esarjJsonPath))
                ParseEsarjJson(esarjJsonPath, stations);

            var sharzJsonPath = Path.Combine(dataPath, "sharz-ovolt.json");
            if (File.Exists(sharzJsonPath))
                ParseSharzOvoltJson(sharzJsonPath, stations);

            if (stations.Count > 0)
            {
                context.Stations.AddRange(stations);
                context.SaveChanges();
            }
        }
    }

    private static Vehicle V(string id, string brand, string model, int range, double battery) =>
        new Vehicle { Id = Guid.Parse(id), Brand = brand, Model = model, RangeKm = range, BatteryCapacityKWh = battery, AverageConsumptionKWhPer100Km = Math.Round((battery / range) * 100, 2) };

    private static void SeedVehicles(AppDbContext context)
    {
        var v = new List<Vehicle>
        {
            V("a0000001-0000-0000-0000-000000000001","Togg","T10X (88.5 kWh)",523,88.5),
            V("a0000002-0000-0000-0000-000000000001","Togg","T10F (88.5 kWh)",501,88.5),
            V("a0000003-0000-0000-0000-000000000001","Tesla","Model Y Standard Range",345,57.5),
            V("a0000004-0000-0000-0000-000000000001","Tesla","Model Y Long Range",533,75.0),
            V("a0000005-0000-0000-0000-000000000001","Tesla","Model 3 Standard Range",388,57.5),
            V("a0000006-0000-0000-0000-000000000001","Tesla","Model 3 Long Range",629,82.0),
            V("a0000007-0000-0000-0000-000000000001","Tesla","Model X",576,100.0),
            V("a0000008-0000-0000-0000-000000000001","Tesla","Model S Long Range",652,100.0),
            V("a0000009-0000-0000-0000-000000000001","Hyundai","Ioniq 5 Standard (58 kWh)",385,58.0),
            V("a0000010-0000-0000-0000-000000000001","Hyundai","Ioniq 5 Long Range (77 kWh)",481,72.6),
            V("a0000011-0000-0000-0000-000000000001","Hyundai","Ioniq 6 Long Range",614,77.4),
            V("a0000012-0000-0000-0000-000000000001","Hyundai","Kona Electric 64 kWh",454,64.0),
            V("a0000013-0000-0000-0000-000000000001","Kia","EV6 Standard Range",394,58.0),
            V("a0000014-0000-0000-0000-000000000001","Kia","EV6 Long Range",528,77.4),
            V("a0000015-0000-0000-0000-000000000001","Kia","EV9 Long Range",563,99.8),
            V("a0000016-0000-0000-0000-000000000001","Kia","Niro EV",463,64.8),
            V("a0000017-0000-0000-0000-000000000001","BMW","i3 (42.2 kWh)",285,42.2),
            V("a0000018-0000-0000-0000-000000000001","BMW","i4 eDrive40",590,83.9),
            V("a0000019-0000-0000-0000-000000000001","BMW","i4 M50",510,83.9),
            V("a0000020-0000-0000-0000-000000000001","BMW","iX3",461,80.0),
            V("a0000021-0000-0000-0000-000000000001","BMW","iX xDrive40",425,76.6),
            V("a0000022-0000-0000-0000-000000000001","BMW","iX xDrive50",630,111.5),
            V("a0000023-0000-0000-0000-000000000001","MINI","Cooper SE Electric",234,32.6),
            V("a0000024-0000-0000-0000-000000000001","MINI","Cooper Electric (54 kWh)",402,54.2),
            V("a0000025-0000-0000-0000-000000000001","MINI","Aceman E (40.7 kWh)",310,40.7),
            V("a0000026-0000-0000-0000-000000000001","MINI","Aceman SE (54.2 kWh)",406,54.2),
            V("a0000027-0000-0000-0000-000000000001","MINI","Countryman E",462,66.5),
            V("a0000028-0000-0000-0000-000000000001","MINI","Countryman SE ALL4",433,66.5),
            V("a0000029-0000-0000-0000-000000000001","Mercedes","EQA 250+",426,70.5),
            V("a0000030-0000-0000-0000-000000000001","Mercedes","EQB 350 4MATIC",419,66.5),
            V("a0000031-0000-0000-0000-000000000001","Mercedes","EQC 400 4MATIC",414,80.0),
            V("a0000032-0000-0000-0000-000000000001","Mercedes","EQE 350+",654,90.6),
            V("a0000033-0000-0000-0000-000000000001","Mercedes","EQS 450+",770,107.8),
            V("a0000034-0000-0000-0000-000000000001","Audi","Q4 e-tron 40",520,82.0),
            V("a0000035-0000-0000-0000-000000000001","Audi","Q4 e-tron 50 quattro",488,82.0),
            V("a0000036-0000-0000-0000-000000000001","Audi","Q8 e-tron 55",582,114.0),
            V("a0000037-0000-0000-0000-000000000001","Audi","e-tron GT",488,93.4),
            V("a0000038-0000-0000-0000-000000000001","Audi","RS e-tron GT",472,93.4),
            V("a0000039-0000-0000-0000-000000000001","Volkswagen","ID.3 Pro (58 kWh)",426,58.0),
            V("a0000040-0000-0000-0000-000000000001","Volkswagen","ID.3 Pro S (77 kWh)",549,77.0),
            V("a0000041-0000-0000-0000-000000000001","Volkswagen","ID.4 Pro (77 kWh)",522,77.0),
            V("a0000042-0000-0000-0000-000000000001","Volkswagen","ID.4 GTX AWD",490,77.0),
            V("a0000043-0000-0000-0000-000000000001","Volkswagen","ID.5 GTX",490,77.0),
            V("a0000044-0000-0000-0000-000000000001","Volkswagen","ID.7 Pro S",709,91.0),
            V("a0000045-0000-0000-0000-000000000001","Porsche","Taycan 4S",484,93.4),
            V("a0000046-0000-0000-0000-000000000001","Porsche","Taycan Turbo",435,93.4),
            V("a0000047-0000-0000-0000-000000000001","Porsche","Macan Electric",516,100.0),
            V("a0000048-0000-0000-0000-000000000001","Peugeot","e-208 (50 kWh)",362,50.0),
            V("a0000049-0000-0000-0000-000000000001","Peugeot","e-2008 (50 kWh)",340,50.0),
            V("a0000050-0000-0000-0000-000000000001","Peugeot","e-308 (54 kWh)",416,54.0),
            V("a0000051-0000-0000-0000-000000000001","Renault","Zoe ZE50",386,52.0),
            V("a0000052-0000-0000-0000-000000000001","Renault","Megane E-Tech 60 kWh",450,60.0),
            V("a0000053-0000-0000-0000-000000000001","Renault","Scenic E-Tech 87 kWh",620,87.0),
            V("a0000054-0000-0000-0000-000000000001","Fiat","500e (42 kWh)",321,42.0),
            V("a0000055-0000-0000-0000-000000000001","Fiat","600e",409,54.0),
            V("a0000056-0000-0000-0000-000000000001","Opel","Corsa-e (50 kWh)",359,50.0),
            V("a0000057-0000-0000-0000-000000000001","Opel","Mokka-e (50 kWh)",338,50.0),
            V("a0000058-0000-0000-0000-000000000001","Opel","Astra Electric",416,54.0),
            V("a0000059-0000-0000-0000-000000000001","Cupra","Born (58 kWh)",424,58.0),
            V("a0000060-0000-0000-0000-000000000001","Cupra","Born (77 kWh)",570,77.0),
            V("a0000061-0000-0000-0000-000000000001","Cupra","Tavascan VZ",517,77.0),
            V("a0000062-0000-0000-0000-000000000001","Skoda","Enyaq 60",412,62.0),
            V("a0000063-0000-0000-0000-000000000001","Skoda","Enyaq 85",572,82.0),
            V("a0000064-0000-0000-0000-000000000001","Volvo","C40 Recharge",476,82.0),
            V("a0000065-0000-0000-0000-000000000001","Volvo","EX30 Single Motor",344,51.0),
            V("a0000066-0000-0000-0000-000000000001","Volvo","EX40 Single Motor",473,82.0),
            V("a0000067-0000-0000-0000-000000000001","Volvo","EX90 Twin Motor",580,111.0),
            V("a0000068-0000-0000-0000-000000000001","Polestar","Polestar 2 Standard",490,69.0),
            V("a0000069-0000-0000-0000-000000000001","Polestar","Polestar 2 Long Range",592,82.0),
            V("a0000070-0000-0000-0000-000000000001","Polestar","Polestar 3",561,111.0),
            V("a0000071-0000-0000-0000-000000000001","Polestar","Polestar 4",560,100.0),
            V("a0000072-0000-0000-0000-000000000001","Lexus","UX 300e",315,72.8),
            V("a0000073-0000-0000-0000-000000000001","Lexus","RZ 450e",440,71.4),
            V("a0000074-0000-0000-0000-000000000001","BYD","Atto 3 (60.5 kWh)",420,60.5),
            V("a0000075-0000-0000-0000-000000000001","BYD","Seal (82.5 kWh)",570,82.5),
            V("a0000076-0000-0000-0000-000000000001","BYD","Han (85.4 kWh)",521,85.4),
            V("a0000077-0000-0000-0000-000000000001","BYD","Dolphin (60.4 kWh)",427,60.4),
            V("a0000078-0000-0000-0000-000000000001","Citroen","e-C4 (54 kWh)",420,54.0),
            V("a0000079-0000-0000-0000-000000000001","Nissan","Leaf (40 kWh)",270,40.0),
            V("a0000080-0000-0000-0000-000000000001","Nissan","Leaf e+ (62 kWh)",385,62.0),
            V("a0000081-0000-0000-0000-000000000001","Nissan","Ariya 87 kWh",533,87.0),
            V("a0000082-0000-0000-0000-000000000001","Togg","T10X Standard Range (52.4 kWh)",314,52.4),
            V("a0000083-0000-0000-0000-000000000001","Togg","T10F Standard Range (52.4 kWh)",350,52.4),
            V("a0000084-0000-0000-0000-000000000001","Tesla","Model Y Performance",514,75.0),
            V("a0000085-0000-0000-0000-000000000001","Tesla","Model 3 Performance",528,82.0),
            V("a0000086-0000-0000-0000-000000000001","Hyundai","Ioniq 5 AWD (77.4 kWh)",430,77.4),
            V("a0000087-0000-0000-0000-000000000001","BMW","i4 eDrive35",483,70.2),
            V("a0000088-0000-0000-0000-000000000001","MG","MG4 Standard Range",350,51.0),
            V("a0000089-0000-0000-0000-000000000001","MG","MG4 Luxury/Long Range",435,64.0),
            V("a0000090-0000-0000-0000-000000000001","MG","ZS EV Standard Range",320,51.0),
            V("a0000091-0000-0000-0000-000000000001","MG","ZS EV Long Range",440,72.0),
            V("a0000092-0000-0000-0000-000000000001","Renault","Megane E-Tech 40 kWh",300,40.0),
            V("a0000093-0000-0000-0000-000000000001","Volvo","XC40 Recharge Twin",530,82.0),
            V("a0000094-0000-0000-0000-000000000001","Dacia","Spring",230,26.8),
            V("a0000095-0000-0000-0000-000000000001","Peugeot","e-208 (54 kWh)",400,54.0),
            V("a0000096-0000-0000-0000-000000000001","BYD","Seal U EV Comfort (71.8 kWh)",420,71.8),
            V("a0000097-0000-0000-0000-000000000001","BYD","Seal U EV Design (87 kWh)",500,87.0),
            V("a0000098-0000-0000-0000-000000000001","Fiat","Grande Panda Electric",320,44.0),
            V("a0000099-0000-0000-0000-000000000001","Hyundai","Inster Standard Range",300,42.0),
            V("a0000100-0000-0000-0000-000000000001","Hyundai","Inster Long Range",355,49.0),
            V("a0000101-0000-0000-0000-000000000001","Citroen","e-C3",320,44.0)
        };
        context.Vehicles.AddRange(v);
        context.SaveChanges();
    }


    // ─── ZES / stations.json parser ───────────────────────────────────────────
    private static void ParseStationsJson(string path, List<Station> result)
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<StationDataRoot>(File.ReadAllText(path), opts);
        if (data == null) return;

        if (data.StationLocations != null)
        {
            foreach (var s in data.StationLocations)
            {
                result.Add(new Station
                {
                    Id                = Guid.NewGuid(),
                    Name              = NonEmpty(s.Name, "Bilinmeyen İstasyon"),
                    Latitude          = s.Latitude,
                    Longitude         = s.Longitude,
                    Brand             = "ZES",
                    IsFastCharge      = s.DcConnectorCount > 0 || s.HpcConnectorCount > 0,
                    AcConnectorCount  = s.AcConnectorCount,
                    DcConnectorCount  = s.DcConnectorCount,
                    HpcConnectorCount = s.HpcConnectorCount,
                    MaxPowerKw        = s.MaxElectricPower > 0 ? s.MaxElectricPower : null,
                });
            }
        }

        if (data.Stations != null)
        {
            foreach (var s in data.Stations)
            {
                if (IsDuplicate(result, s.Latitude, s.Longitude)) continue;
                result.Add(new Station
                {
                    Id                = Guid.NewGuid(),
                    Name              = NonEmpty(s.Name, "Bilinmeyen İstasyon"),
                    Latitude          = s.Latitude,
                    Longitude         = s.Longitude,
                    Brand             = "ZES",
                    IsFastCharge      = s.DcConnectorCount > 0 || s.HpcConnectorCount > 0,
                    AcConnectorCount  = s.AcConnectorCount,
                    DcConnectorCount  = s.DcConnectorCount,
                    HpcConnectorCount = s.HpcConnectorCount,
                    MaxPowerKw        = s.MaxElectricPower > 0 ? s.MaxElectricPower : null,
                });
            }
        }
    }

    // ─── Trugo GeoJSON parser ─────────────────────────────────────────────────
    // Beklenen yapı:
    // { "status":"OK", "data": { "stationList": { "type":"FeatureCollection", "features": [...] } } }
    // Her feature:
    // { "type":"Feature", "geometry":{"type":"Point","coordinates":[lng,lat]},
    //   "properties":{"name":"...", "acConnectorCount":0, "dcConnectorCount":2, ... } }
    private static void ParseTrugoGeoJson(string path, List<Station> result)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        // Gerçek Trugo yapısı: { status, data: { stationList: { type:"FeatureCollection", features:[...] } } }
        JsonElement features;
        try
        {
            features = root
                .GetProperty("data")
                .GetProperty("stationList")
                .GetProperty("features");
        }
        catch
        {
            if (!root.TryGetProperty("features", out features)) return;
        }

        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("geometry", out var geom)) continue;
            if (!feature.TryGetProperty("properties", out var props)) continue;

            // GeoJSON: coordinates = [lng, lat]
            var coords = geom.GetProperty("coordinates");
            var lng = coords[0].GetDouble();
            var lat = coords[1].GetDouble();

            if (IsDuplicate(result, lat, lng)) continue;

            // Trugo: isim alanı "locationName"
            string? name = null;
            if (props.TryGetProperty("locationName", out var locProp)) name = locProp.GetString();
            else if (props.TryGetProperty("name", out var nProp))       name = nProp.GetString();

            // Trugo: "type" alanı "AC" / "DC" / "HPC" / "AC+DC" gibi string
            string? typeStr = null;
            if (props.TryGetProperty("type", out var tProp) && tProp.ValueKind == JsonValueKind.String)
                typeStr = tProp.GetString()?.ToUpperInvariant();

            // Eğer sayısal connectorCount varsa önceliklendir, yoksa type string'den türet
            int ac  = 0, dc = 0, hpc = 0;
            if (props.TryGetProperty("acConnectorCount",  out var acP)  && acP.ValueKind  == JsonValueKind.Number) ac  = acP.GetInt32();
            if (props.TryGetProperty("dcConnectorCount",  out var dcP)  && dcP.ValueKind  == JsonValueKind.Number) dc  = dcP.GetInt32();
            if (props.TryGetProperty("hpcConnectorCount", out var hpcP) && hpcP.ValueKind == JsonValueKind.Number) hpc = hpcP.GetInt32();

            if (ac == 0 && dc == 0 && hpc == 0 && typeStr != null)
            {
                // type string'den tahmin et
                if (typeStr.Contains("HPC"))            hpc = 1;
                else if (typeStr.Contains("DC"))        dc  = 1;
                else if (typeStr.Contains("AC"))        ac  = 1;
                else                                   dc  = 1; // bilinmeyen = DC kabul
            }

            int? maxPowerKw = null;
            if (hpc > 0) maxPowerKw = 180;
            else if (dc > 0) maxPowerKw = 120;
            else if (ac > 0) maxPowerKw = 22;

            result.Add(new Station
            {
                Id                = Guid.NewGuid(),
                Name              = NonEmpty(name, "Trugo İstasyonu"),
                Latitude          = lat,
                Longitude         = lng,
                Brand             = "Trugo",
                IsFastCharge      = dc > 0 || hpc > 0,
                AcConnectorCount  = ac,
                DcConnectorCount  = dc,
                HpcConnectorCount = hpc,
                MaxPowerKw        = maxPowerKw,
            });
        }
    }

    // ─── Eşarj JSON parser ───────────────────────────────────────────────────
    private static void ParseEsarjJson(string path, List<Station> result)
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<EsarjRoot>(File.ReadAllText(path), opts);
        if (data?.Data == null) return;

        foreach (var s in data.Data)
        {
            if (IsDuplicate(result, s.Latitude, s.Longitude)) continue;
            
            int maxPower = 0;
            if (s.ConnectorNominalPowers != null)
            {
                foreach(var el in s.ConnectorNominalPowers) 
                {
                    if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out double d)) 
                        maxPower = Math.Max(maxPower, (int)d);
                    else if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out double ds))
                        maxPower = Math.Max(maxPower, (int)ds);
                }
            }

            int hpc = 0, dc = s.DcConnectors;
            if (maxPower >= 150 && dc > 0)
            {
                hpc = dc;
                dc = 0;
            }

            result.Add(new Station
            {
                Id                = Guid.NewGuid(),
                Name              = NonEmpty(s.StoreName, "Eşarj İstasyonu"),
                Latitude          = s.Latitude,
                Longitude         = s.Longitude,
                Brand             = "Eşarj",
                IsFastCharge      = dc > 0 || hpc > 0,
                AcConnectorCount  = s.AcConnectors,
                DcConnectorCount  = dc,
                HpcConnectorCount = hpc,
                MaxPowerKw        = maxPower > 0 ? maxPower : null,
            });
        }
    }

    // ─── Sharz / Ovolt JSON parser ───────────────────────────────────────────
    private static void ParseSharzOvoltJson(string path, List<Station> result)
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<SharzOvoltRoot>(File.ReadAllText(path), opts);
        if (data?.Result?.Stations == null) return;

        foreach (var s in data.Result.Stations)
        {
            if (IsDuplicate(result, s.Lat, s.Lon)) continue;
            
            string brand = s.CompanyId == 2 ? "Ovolt" : (s.CompanyId == 3 ? "Sharz.net" : "Diğer");
            
            int maxPower = 0;
            if (s.Powers != null)
            {
                foreach(var el in s.Powers) 
                {
                    if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out double d)) 
                        maxPower = Math.Max(maxPower, (int)d);
                    else if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out double ds))
                        maxPower = Math.Max(maxPower, (int)ds);
                }
            }
            
            int ac = GetTotalFromText(s.Ac?.Text);
            int dcRaw = GetTotalFromText(s.Dc?.Text);
            
            int hpc = 0, dc = dcRaw;
            if (maxPower >= 150 && dc > 0)
            {
                hpc = dc;
                dc = 0;
            }

            result.Add(new Station
            {
                Id                = Guid.NewGuid(),
                Name              = NonEmpty(s.Name, brand + " İstasyonu"),
                Latitude          = s.Lat,
                Longitude         = s.Lon,
                Brand             = brand,
                IsFastCharge      = dc > 0 || hpc > 0,
                AcConnectorCount  = ac,
                DcConnectorCount  = dc,
                HpcConnectorCount = hpc,
                MaxPowerKw        = maxPower > 0 ? maxPower : null,
            });
        }
    }
    
    private static int GetTotalFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var parts = text.Split('/');
        if (parts.Length == 2 && int.TryParse(parts[1], out int total)) return total;
        return 0;
    }

    // ─── Yardımcı metodlar ────────────────────────────────────────────────────

    private static bool IsDuplicate(List<Station> list, double lat, double lng) =>
        list.Any(s => Math.Abs(s.Latitude - lat) < 0.0001 && Math.Abs(s.Longitude - lng) < 0.0001);

    private static string NonEmpty(string? s, string fallback) =>
        string.IsNullOrWhiteSpace(s) ? fallback : s;
}

// ── Deserialization modelleri ──────────────────────────────────────────────────

public class StationDataRoot
{
    public List<StationLocationJson>? StationLocations { get; set; }
    public List<StationJson>?         Stations         { get; set; }
}

public class StationLocationJson
{
    public string? Name              { get; set; }
    public double  Latitude          { get; set; }
    public double  Longitude         { get; set; }
    public bool    IsZesStation      { get; set; }
    public int     AcConnectorCount  { get; set; }
    public int     DcConnectorCount  { get; set; }
    public int     HpcConnectorCount { get; set; }
    public int     MaxElectricPower  { get; set; }
}

public class StationJson
{
    public string? Name              { get; set; }
    public double  Latitude          { get; set; }
    public double  Longitude         { get; set; }
    public int     AcConnectorCount  { get; set; }
    public int     DcConnectorCount  { get; set; }
    public int     HpcConnectorCount { get; set; }
    public int     MaxElectricPower  { get; set; }
}

public class EsarjRoot
{
    public List<EsarjStation>? Data { get; set; }
}
public class EsarjStation
{
    public string? StoreName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int AcConnectors { get; set; }
    public int DcConnectors { get; set; }
    public List<JsonElement>? ConnectorNominalPowers { get; set; }
}

public class SharzOvoltRoot
{
    public SharzOvoltResult? Result { get; set; }
}
public class SharzOvoltResult
{
    public List<SharzOvoltStation>? Stations { get; set; }
}
public class SharzOvoltStation
{
    public string? Name { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public int CompanyId { get; set; }
    public List<JsonElement>? Powers { get; set; }
    public SharzOvoltConnector? Ac { get; set; }
    public SharzOvoltConnector? Dc { get; set; }
}
public class SharzOvoltConnector
{
    public string? Text { get; set; }
}
