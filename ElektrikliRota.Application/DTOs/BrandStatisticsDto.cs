namespace ElektrikliRota.Application.DTOs;

public class BrandStatisticsDto
{
    public string BrandName { get; set; } = string.Empty;
    public string BrandSlug { get; set; } = string.Empty;
    public int TotalStations { get; set; }
    public int TotalAcConnectors { get; set; }
    public int TotalDcConnectors { get; set; }
    public int MaxPowerKw { get; set; }
    public List<string> TopCities { get; set; } = new();
    public BrandPricingDto? Pricing { get; set; }
}

public class BrandPricingDto
{
    public double AC { get; set; }
    public double DC { get; set; }
    public string? Note { get; set; }
}
