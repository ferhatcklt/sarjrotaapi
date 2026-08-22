using System;
using System.Collections.Generic;
using System.Net.Http;
using ElektrikliRota.Core.Entities;
using ElektrikliRota.Core.Interfaces;
using ElektrikliRota.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ElektrikliRota.UnitTests;

public class RouteServiceTests
{
    private readonly RouteService _routeService;

    public RouteServiceTests()
    {
        var stationRepoMock = new Mock<IStationRepository>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var configMock = new Mock<IConfiguration>();
        _routeService = new RouteService(stationRepoMock.Object, httpClientFactoryMock.Object, configMock.Object);
    }

    [Fact]
    public void BuildChargeStops_Should_Return_Null_When_No_Stations_Found_In_Range()
    {
        // Arrange — ~50 km segmentler kullanarak eşiğin pozitif menzilde tetiklenmesini sağlıyoruz
        // realMax = 200 * 0.90 / 1.10 ≈ 163.6 km, threshold ≈ 16.4 km
        // 3. segmentten sonra ~150 km → currentRange ≈ 13.6 < 16.4 → tetiklenir
        var path = new List<Location>
        {
            new Location { Latitude = 41.00, Longitude = 29.0 },  // Başlangıç
            new Location { Latitude = 41.45, Longitude = 29.0 },  // ~50 km
            new Location { Latitude = 41.90, Longitude = 29.0 },  // ~100 km
            new Location { Latitude = 42.35, Longitude = 29.0 },  // ~150 km → eşik tetiklenir
            new Location { Latitude = 42.80, Longitude = 29.0 }   // ~200 km
        };
        var distKm = 200;
        var durSec = 7200; // ~100 km/h → consumptionMultiplier = 1.10

        var vehicle = new Vehicle { RangeKm = 200, BatteryCapacityKWh = 50, AverageConsumptionKWhPer100Km = 25 };
        var availableStations = new List<Station>(); // Hiç istasyon yok

        // Act
        var (stops, arrivalCharge) = _routeService.BuildChargeStops(path, distKm, durSec, vehicle, availableStations, 100, 0);

        // Assert — İstasyon bulunamadığı için null dönmeli
        stops.Should().BeNull();
        arrivalCharge.Should().Be(0);
    }

    [Fact]
    public void BuildChargeStops_Should_Add_Stop_When_Range_Is_Critical()
    {
        // Arrange — Aynı granüler yol, ama bu sefer eşik noktasında bir istasyon var
        var path = new List<Location>
        {
            new Location { Latitude = 41.00, Longitude = 29.0 },
            new Location { Latitude = 41.45, Longitude = 29.0 },
            new Location { Latitude = 41.90, Longitude = 29.0 },
            new Location { Latitude = 42.35, Longitude = 29.0 },  // Eşik burada tetiklenir
            new Location { Latitude = 42.80, Longitude = 29.0 }
        };
        var distKm = 200;
        var durSec = 7200; // ~100 km/h

        var vehicle = new Vehicle { RangeKm = 200, BatteryCapacityKWh = 50, AverageConsumptionKWhPer100Km = 25 };

        // İstasyon: eşik noktasına (~42.35) yakın, rotadan ~11 km uzakta (< 40 km arama yarıçapı)
        var availableStations = new List<Station>
        {
            new Station { Id = Guid.NewGuid(), Latitude = 42.35, Longitude = 29.1, Brand = "ZES", IsFastCharge = true }
        };

        // Act
        var (stops, arrivalCharge) = _routeService.BuildChargeStops(path, distKm, durSec, vehicle, availableStations, 100, 0);

        // Assert — İstasyon bulunup durak olarak eklenmeli
        stops.Should().NotBeNull();
        stops.Should().HaveCount(1);
        stops![0].Brand.Should().Be("ZES");
    }

    [Fact]
    public void BuildChargeStops_Should_Return_Empty_When_Range_Sufficient()
    {
        // Arrange — Kısa mesafe, şarj durağı gerekmemeli
        var path = new List<Location>
        {
            new Location { Latitude = 41.0, Longitude = 29.0 },
            new Location { Latitude = 41.3, Longitude = 29.0 }   // ~33 km
        };
        var distKm = 33;
        var durSec = 1200; // ~100 km/h

        var vehicle = new Vehicle { RangeKm = 200, BatteryCapacityKWh = 50, AverageConsumptionKWhPer100Km = 25 };
        var availableStations = new List<Station>();

        // Act
        var (stops, arrivalCharge) = _routeService.BuildChargeStops(path, distKm, durSec, vehicle, availableStations, 100, 0);

        // Assert — Menzil yeterli, durak eklenmemeli ve varış şarjı > 0
        stops.Should().NotBeNull();
        stops.Should().BeEmpty();
        arrivalCharge.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateRouteAsync_Should_Return_Valid_Route_When_OSRM_Responds()
    {
        var httpClient = new HttpClient();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var stationRepoMock = new Mock<IStationRepository>();
        stationRepoMock.Setup(r => r.GetAllStationsAsync()).ReturnsAsync(new List<Station>());

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["OsrmSettings:BaseUrl"]).Returns("https://router.project-osrm.org");

        var service = new RouteService(stationRepoMock.Object, httpClientFactoryMock.Object, configMock.Object);

        var start = new Location { Latitude = 41.0082, Longitude = 28.9784 }; // Istanbul
        var end   = new Location { Latitude = 40.9801, Longitude = 29.0823 }; // Kadikoy (short trip)
        var vehicle = new Vehicle { RangeKm = 400, BatteryCapacityKWh = 75 };

        var result = await service.CalculateRouteAsync(start, end, vehicle, new(), new(), 100, 0);

        result.Should().NotBeNull();
        result.Path.Should().NotBeEmpty();
        result.TotalDistanceKm.Should().BeGreaterThan(0);
        result.EstimatedDurationHours.Should().BeGreaterThan(0);
    }
}
