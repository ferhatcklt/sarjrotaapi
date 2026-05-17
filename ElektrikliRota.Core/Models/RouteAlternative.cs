using ElektrikliRota.Core.Entities;

namespace ElektrikliRota.Core.Models;

/// <summary>Tek bir rota alternatifini temsil eder.</summary>
public class RouteAlternative
{
    public int Index { get; set; }
    public List<Location> Path { get; set; } = new();
    /// <summary>Rota planındaki zorunlu şarj durakları (araç bu noktalara uğrar).</summary>
    public List<Station> Stops { get; set; } = new();
    /// <summary>Rota koridoru boyunca 30 km içindeki TÜM istasyonlar (bilgi amaçlı pin).</summary>
    public List<Station> NearbyStations { get; set; } = new();
    public double TotalDistanceKm { get; set; }
    public double EstimatedDurationHours { get; set; }
    /// <summary>Toplam şarj bekleme süresi (saat).</summary>
    public double ChargeTimeHours { get; set; }
    /// <summary>Sürüş + şarj toplam seyahat süresi.</summary>
    public double TotalJourneyHours => Math.Round(EstimatedDurationHours + ChargeTimeHours, 2);
    public int ChargeStopsCount => Stops.Count;
    /// <summary>Varış noktasına ulaşıldığında tahmini kalan şarj yüzdesi.</summary>
    public int ArrivalChargePercentage { get; set; }
    /// <summary>Rotanın tahmini şarj maliyeti (TL).</summary>
    public double EstimatedCost { get; set; }
}
