using ElektrikliRota.Core.Entities;
using ElektrikliRota.Core.Interfaces;

namespace ElektrikliRota.Application.Services;

public class VehicleAppService
{
    private readonly IVehicleRepository _vehicleRepository;

    public VehicleAppService(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<List<Vehicle>> GetAllVehiclesAsync()
    {
        return await _vehicleRepository.GetAllAsync();
    }
}
