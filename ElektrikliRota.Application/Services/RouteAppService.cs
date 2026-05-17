using ElektrikliRota.Application.DTOs;
using ElektrikliRota.Core.Entities;
using ElektrikliRota.Core.Interfaces;
using ElektrikliRota.Core.Models;

namespace ElektrikliRota.Application.Services;

public class RouteAppService
{
    private readonly IRouteService _routeService;
    private readonly IVehicleRepository _vehicleRepository;

    public RouteAppService(IRouteService routeService, IVehicleRepository vehicleRepository)
    {
        _routeService = routeService;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<RouteResult> GetOptimizedRouteAsync(RouteRequestDto request)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId);
        if (vehicle == null) throw new Exception("Vehicle not found");

        var startLocation = new Location { Latitude = request.Start.Lat, Longitude = request.Start.Lng };
        var endLocation = new Location { Latitude = request.End.Lat, Longitude = request.End.Lng };

        return await _routeService.CalculateRouteAsync(startLocation, endLocation, vehicle, request.PreferredBrands, request.ConnectorTypes, request.InitialChargePercentage);
    }
}
