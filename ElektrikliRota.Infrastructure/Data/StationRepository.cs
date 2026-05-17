using ElektrikliRota.Core.Entities;
using ElektrikliRota.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElektrikliRota.Infrastructure.Data;

public class StationRepository : IStationRepository
{
    private readonly AppDbContext _context;

    public StationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Station>> GetAllStationsAsync()
    {
        return await _context.Stations.ToListAsync();
    }

    public async Task<List<Station>> GetStationsByBrandsAsync(List<string> brands)
    {
        if (brands == null || !brands.Any())
            return await _context.Stations.ToListAsync();

        return await _context.Stations.Where(s => brands.Contains(s.Brand)).ToListAsync();
    }
}
