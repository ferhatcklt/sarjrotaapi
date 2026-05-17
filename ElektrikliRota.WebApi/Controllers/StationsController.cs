using ElektrikliRota.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElektrikliRota.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StationsController : ControllerBase
{
    private readonly StationAppService _stationAppService;

    public StationsController(StationAppService stationAppService)
    {
        _stationAppService = stationAppService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStations()
    {
        var stations = await _stationAppService.GetAllStationsAsync();
        return Ok(stations);
    }

    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands()
    {
        var stations = await _stationAppService.GetAllStationsAsync();
        var brands = stations
            .Select(s => s.Brand)
            .Where(b => !string.IsNullOrEmpty(b))
            .Distinct()
            .OrderBy(b => b)
            .ToList();
        return Ok(brands);
    }
}
