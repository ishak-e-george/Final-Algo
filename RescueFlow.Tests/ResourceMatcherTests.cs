using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RescueFlow.Web.Data;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Entities;
using RescueFlow.Web.Models.Enums;
using RescueFlow.Web.Services;
using Xunit;

namespace RescueFlow.Tests;

public class ResourceMatcherTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RescueFlowDbContext _context;
    private readonly ResourceMatcher _matcher;

    public class MockRoutingService : IRoutingService
    {
        public Task<RouteEstimate> EstimateAsync(Location from, Location to)
        {
            // Simple straight-line distance representation as ETA
            double latDiff = from.Latitude - to.Latitude;
            double lngDiff = from.Longitude - to.Longitude;
            double dist = Math.Sqrt(latDiff * latDiff + lngDiff * lngDiff) * 100.0;
            return Task.FromResult(new RouteEstimate(dist, dist * 1.5));
        }
    }

    public ResourceMatcherTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<RescueFlowDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new RescueFlowDbContext(options);
        _context.Database.EnsureCreated();

        _matcher = new ResourceMatcher(_context, new MockRoutingService());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task SeedVehiclesAsync()
    {
        var station1 = new Station { Id = 1, Name = "North Station", Latitude = 40.8000, Longitude = -74.0000, IsActive = true };
        var station2 = new Station { Id = 2, Name = "South Station", Latitude = 40.7000, Longitude = -74.0000, IsActive = true };
        _context.Stations.AddRange(station1, station2);

        var equip = new Equipment { Id = 1, Name = "Trauma Bag" };
        _context.Equipment.Add(equip);
        await _context.SaveChangesAsync();

        var v1 = new RescueVehicle
        {
            Id = 1,
            Code = "AMB-NORTH",
            StationId = 1,
            VehicleType = VehicleType.Ambulance,
            Status = VehicleStatus.Available,
            CurrentCrewCount = 2,
            MaxCrewCapacity = 2,
            CurrentLatitude = 40.8000,
            CurrentLongitude = -74.0000
        };

        var v2 = new RescueVehicle
        {
            Id = 2,
            Code = "AMB-SOUTH",
            StationId = 2,
            VehicleType = VehicleType.Ambulance,
            Status = VehicleStatus.Available,
            CurrentCrewCount = 2,
            MaxCrewCapacity = 2,
            CurrentLatitude = 40.7000,
            CurrentLongitude = -74.0000
        };

        _context.RescueVehicles.AddRange(v1, v2);
        await _context.SaveChangesAsync();

        _context.VehicleEquipment.AddRange(
            new VehicleEquipment { RescueVehicleId = 1, EquipmentId = 1 },
            new VehicleEquipment { RescueVehicleId = 2, EquipmentId = 1 }
        );
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task FindEligibleVehicles_ShouldPickClosestVehicleBasedOnEta()
    {
        await SeedVehiclesAsync();

        // Case is close to South Station (40.7100, -74.0000)
        double caseLat = 40.7100;
        double caseLng = -74.0000;

        var requirements = new List<ResourceRequirement>
        {
            new()
            {
                VehicleType = VehicleType.Ambulance,
                RequiredVehicleCount = 1,
                RequiredPersonnelPerVehicle = 2,
                RequiredEquipment = new List<string> { "Trauma Bag" }
            }
        };

        // Act
        var (selected, rejections) = await _matcher.FindEligibleVehiclesAsync(requirements, caseLat, caseLng);

        // Assert
        Assert.Empty(rejections);
        Assert.Single(selected);
        Assert.Equal("AMB-SOUTH", selected[0].Code);
    }

    [Fact]
    public async Task FindEligibleVehicles_ShouldApplyScarcityPenaltyTieBreaker()
    {
        // Setup a scenario where ETAs are identical, but one station has more backup vehicles.
        // Station 1 (North) has 2 available ambulances.
        // Station 2 (South) has 1 available ambulance.
        // The case is exactly in the middle. We want to see if the matcher selects the one from Station 1 to preserve backup capacity at Station 2.
        
        var station1 = new Station { Id = 1, Name = "North Station", Latitude = 40.8000, Longitude = -74.0000, IsActive = true };
        var station2 = new Station { Id = 2, Name = "South Station", Latitude = 40.6000, Longitude = -74.0000, IsActive = true };
        _context.Stations.AddRange(station1, station2);

        var equip = new Equipment { Id = 1, Name = "Trauma Bag" };
        _context.Equipment.Add(equip);
        await _context.SaveChangesAsync();

        // 2 ambulances at North Station
        var v1 = new RescueVehicle { Id = 1, Code = "AMB-NORTH-1", StationId = 1, VehicleType = VehicleType.Ambulance, Status = VehicleStatus.Available, CurrentCrewCount = 2, CurrentLatitude = 40.8000, CurrentLongitude = -74.0000 };
        var v2 = new RescueVehicle { Id = 2, Code = "AMB-NORTH-2", StationId = 1, VehicleType = VehicleType.Ambulance, Status = VehicleStatus.Available, CurrentCrewCount = 2, CurrentLatitude = 40.8000, CurrentLongitude = -74.0000 };
        // 1 ambulance at South Station
        var v3 = new RescueVehicle { Id = 3, Code = "AMB-SOUTH-1", StationId = 2, VehicleType = VehicleType.Ambulance, Status = VehicleStatus.Available, CurrentCrewCount = 2, CurrentLatitude = 40.6000, CurrentLongitude = -74.0000 };

        _context.RescueVehicles.AddRange(v1, v2, v3);
        await _context.SaveChangesAsync();

        _context.VehicleEquipment.AddRange(
            new VehicleEquipment { RescueVehicleId = 1, EquipmentId = 1 },
            new VehicleEquipment { RescueVehicleId = 2, EquipmentId = 1 },
            new VehicleEquipment { RescueVehicleId = 3, EquipmentId = 1 }
        );
        await _context.SaveChangesAsync();

        // Case is exactly in the middle (40.7000, -74.0000)
        double caseLat = 40.7000;
        double caseLng = -74.0000;

        var requirements = new List<ResourceRequirement>
        {
            new()
            {
                VehicleType = VehicleType.Ambulance,
                RequiredVehicleCount = 1,
                RequiredPersonnelPerVehicle = 2,
                RequiredEquipment = new List<string> { "Trauma Bag" }
            }
        };

        // Act
        var (selected, rejections) = await _matcher.FindEligibleVehiclesAsync(requirements, caseLat, caseLng);

        // Assert
        Assert.Empty(rejections);
        Assert.Single(selected);
        // North Station has 2 available ambulances, South has 1 available ambulance.
        // Therefore, North Station has a lower scarcity penalty (1/2 = 0.5) compared to South Station (1/1 = 1.0).
        // Since ETA distance is identical, it should prefer the one with the lower scarcity penalty, which is North.
        Assert.StartsWith("AMB-NORTH", selected[0].Code);
    }
}
