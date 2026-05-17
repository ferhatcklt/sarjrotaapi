using ElektrikliRota.Core.Entities;
using ElektrikliRota.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElektrikliRota.Infrastructure.Data;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _context;

    public VehicleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Vehicle>> GetAllAsync()
    {
        return await _context.Vehicles.ToListAsync();
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id)
    {
        return await _context.Vehicles.FindAsync(id);
    }
}
