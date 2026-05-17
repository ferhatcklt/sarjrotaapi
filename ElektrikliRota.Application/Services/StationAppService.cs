using ElektrikliRota.Core.Entities;
using ElektrikliRota.Core.Interfaces;

namespace ElektrikliRota.Application.Services;

public class StationAppService
{
    private readonly IStationRepository _stationRepository;

    public StationAppService(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public async Task<List<Station>> GetAllStationsAsync()
    {
        return await _stationRepository.GetAllStationsAsync();
    }
}
