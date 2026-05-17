using ElektrikliRota.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElektrikliRota.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly VehicleAppService _vehicleAppService;

    public VehiclesController(VehicleAppService vehicleAppService)
    {
        _vehicleAppService = vehicleAppService;
    }

    [HttpGet]
    public async Task<IActionResult> GetVehicles()
    {
        var vehicles = await _vehicleAppService.GetAllVehiclesAsync();
        return Ok(vehicles);
    }
}
