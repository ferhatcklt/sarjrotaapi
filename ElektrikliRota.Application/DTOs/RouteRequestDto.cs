using System.ComponentModel.DataAnnotations;

namespace ElektrikliRota.Application.DTOs;

public class RouteRequestDto
{
    [Required]
    public LocationDto Start { get; set; } = new();

    [Required]
    public LocationDto End { get; set; } = new();

    [Required]
    public Guid VehicleId { get; set; }

    [MaxLength(20)]
    public List<string> PreferredBrands { get; set; } = new();

    /// <summary>AC, DC, HPC — empty means all types accepted</summary>
    [MaxLength(10)]
    public List<string> ConnectorTypes { get; set; } = new();

    [Range(1, 100)]
    public int InitialChargePercentage { get; set; } = 100;

    [Range(-50, 100)]
    public int AdditionalConsumptionPercent { get; set; } = 0;
}

public class LocationDto
{
    [Required]
    [Range(-90.0, 90.0, ErrorMessage = "Enlem değeri -90 ile 90 arasında olmalıdır.")]
    public double Lat { get; set; }

    [Required]
    [Range(-180.0, 180.0, ErrorMessage = "Boylam değeri -180 ile 180 arasında olmalıdır.")]
    public double Lng { get; set; }
}
