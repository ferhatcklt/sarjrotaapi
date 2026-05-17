using ElektrikliRota.Application.DTOs;
using ElektrikliRota.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ElektrikliRota.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("RouteApiLimit")]
public class RouteController : ControllerBase
{
    private readonly RouteAppService _routeAppService;

    public RouteController(RouteAppService routeAppService)
    {
        _routeAppService = routeAppService;
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> CalculateRoute([FromBody] RouteRequestDto request)
    {
        try
        {
            var result = await _routeAppService.GetOptimizedRouteAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
