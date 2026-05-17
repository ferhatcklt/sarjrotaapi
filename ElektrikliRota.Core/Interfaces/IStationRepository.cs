using ElektrikliRota.Core.Entities;

namespace ElektrikliRota.Core.Interfaces;

public interface IStationRepository
{
    Task<List<Station>> GetAllStationsAsync();
    Task<List<Station>> GetStationsByBrandsAsync(List<string> brands);
}
