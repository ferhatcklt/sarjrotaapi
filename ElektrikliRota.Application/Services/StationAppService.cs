using ElektrikliRota.Application.DTOs;
using ElektrikliRota.Core.Entities;
using ElektrikliRota.Core.Interfaces;

namespace ElektrikliRota.Application.Services;

public class StationAppService
{
    private readonly IStationRepository _stationRepository;

    public StationAppService(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public async Task<List<Station>> GetAllStationsAsync()
    {
        return await _stationRepository.GetAllStationsAsync();
    }

    public async Task<BrandStatisticsDto?> GetBrandStatisticsAsync(string brandSlug)
    {
        var allStations = await _stationRepository.GetAllStationsAsync();
        
        // Find the actual brand name matching the slug
        var brandGroup = allStations
            .GroupBy(s => s.Brand)
            .FirstOrDefault(g => Slugify(g.Key) == brandSlug);

        if (brandGroup == null) return null;

        var brandName = brandGroup.Key;
        var stations = brandGroup.ToList();

        // Try to find pricing
        BrandPricingDto? pricing = null;
        if (ElektrikliRota.Core.Models.PricingConstants.Prices.TryGetValue(brandName, out var priceModel))
        {
            pricing = new BrandPricingDto
            {
                AC = priceModel.AC,
                DC = priceModel.DC,
                Note = priceModel.Note
            };
        }

        // Extract top cities (assuming first word of station name is often city in TR networks)
        // Or just taking a few distinct words
        var topCities = stations
            .Select(s => s.Name.Split(' ').FirstOrDefault()?.Trim().Trim(',').Trim('-'))
            .Where(c => !string.IsNullOrEmpty(c) && c.Length > 2)
            .GroupBy(c => c!)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        return new BrandStatisticsDto
        {
            BrandName = brandName,
            BrandSlug = brandSlug,
            TotalStations = stations.Count,
            TotalAcConnectors = stations.Sum(s => s.AcConnectorCount),
            TotalDcConnectors = stations.Sum(s => s.DcConnectorCount + s.HpcConnectorCount),
            MaxPowerKw = stations.Max(s => s.MaxPowerKw) ?? 0,
            TopCities = topCities,
            Pricing = pricing
        };
    }

    public static string Slugify(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        
        text = text.ToLowerInvariant();
        text = text.Replace("ş", "s").Replace("ğ", "g").Replace("ı", "i")
                   .Replace("ö", "o").Replace("ç", "c").Replace("ü", "u");
        
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9\s-]", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        text = text.Replace(" ", "-");
        
        return text;
    }
}
