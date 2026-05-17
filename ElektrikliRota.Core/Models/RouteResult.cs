using ElektrikliRota.Core.Entities;

namespace ElektrikliRota.Core.Models;

/// <summary>Tüm alternatifleri kapsayan yanıt.</summary>
public class RouteResult
{
    // Geriye dönük uyumluluk — ilk (en iyi) alternatifin verileri
    public List<Location> Path  { get; set; } = new();
    public List<Station>  Stops { get; set; } = new();
    public List<Station>  NearbyStations { get; set; } = new();
    public double TotalDistanceKm       { get; set; }
    public double EstimatedDurationHours { get; set; }
    public double ChargeTimeHours        { get; set; }
    public double TotalJourneyHours => Math.Round(EstimatedDurationHours + ChargeTimeHours, 2);
    public int    ChargeStopsCount => Stops.Count;
    /// <summary>Varış noktasına ulaşıldığında tahmini kalan şarj yüzdesi.</summary>
    public int    ArrivalChargePercentage { get; set; }
    /// <summary>Rotanın tahmini şarj maliyeti (TL).</summary>
    public double EstimatedCost { get; set; }

    // Tüm alternatifler (1-3 arası)
    public List<RouteAlternative> Alternatives { get; set; } = new();
}
