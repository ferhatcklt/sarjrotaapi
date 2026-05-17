using ElektrikliRota.Core.Entities;
using ElektrikliRota.Core.Models;

namespace ElektrikliRota.Core.Interfaces;

public interface IRouteService
{
    Task<RouteResult> CalculateRouteAsync(Location start, Location end, Vehicle vehicle, List<string> preferredBrands, List<string> connectorTypes, int initialCharge);
}
