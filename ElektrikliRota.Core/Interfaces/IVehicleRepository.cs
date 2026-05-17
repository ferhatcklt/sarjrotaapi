using ElektrikliRota.Core.Entities;

namespace ElektrikliRota.Core.Interfaces;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id);
    Task<List<Vehicle>> GetAllAsync();
}
