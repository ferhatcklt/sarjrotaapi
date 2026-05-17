namespace ElektrikliRota.Core.Entities;

public class Station
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsFastCharge { get; set; }
    public int AcConnectorCount { get; set; }
    public int DcConnectorCount { get; set; }
    public int HpcConnectorCount { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int? ArrivalChargePercentage { get; set; }
}
