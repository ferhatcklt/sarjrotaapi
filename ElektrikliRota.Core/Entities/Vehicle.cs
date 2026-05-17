namespace ElektrikliRota.Core.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int RangeKm { get; set; }
    public double BatteryCapacityKWh { get; set; }
    public double AverageConsumptionKWhPer100Km { get; set; }
}
