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

public class HospitalMatcherTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RescueFlowDbContext _context;
    private readonly HospitalMatcher _matcher;

    public class MockRoutingService : IRoutingService
    {
        public Task<RouteEstimate> EstimateAsync(Location from, Location to)
        {
            double latDiff = from.Latitude - to.Latitude;
            double lngDiff = from.Longitude - to.Longitude;
            double dist = Math.Sqrt(latDiff * latDiff + lngDiff * lngDiff) * 100.0;
            return Task.FromResult(new RouteEstimate(dist, dist * 1.5));
        }
    }

    public HospitalMatcherTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<RescueFlowDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new RescueFlowDbContext(options);
        _context.Database.EnsureCreated();

        _matcher = new HospitalMatcher(_context, new MockRoutingService());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task SeedHospitalsAsync()
    {
        var h1 = new Hospital { Id = 1, Name = "North Hospital", Latitude = 40.8000, Longitude = -74.0000, IsActive = true };
        var h2 = new Hospital { Id = 2, Name = "South Hospital", Latitude = 40.7000, Longitude = -74.0000, IsActive = true };
        _context.Hospitals.AddRange(h1, h2);
        await _context.SaveChangesAsync();

        _context.HospitalCapabilities.AddRange(
            new HospitalCapabilityEntity { HospitalId = 1, Capability = HospitalCapability.ICU },
            new HospitalCapabilityEntity { HospitalId = 1, Capability = HospitalCapability.GeneralEmergency },
            new HospitalCapabilityEntity { HospitalId = 2, Capability = HospitalCapability.ICU },
            new HospitalCapabilityEntity { HospitalId = 2, Capability = HospitalCapability.GeneralEmergency }
        );

        _context.HospitalCapacities.AddRange(
            new HospitalCapacity { HospitalId = 1, IcuCapacity = 5, CurrentIcuLoad = 1, EmergencyCapacity = 10, CurrentEmergencyLoad = 0 },
            new HospitalCapacity { HospitalId = 2, IcuCapacity = 5, CurrentIcuLoad = 4, EmergencyCapacity = 10, CurrentEmergencyLoad = 0 } // High ICU load
        );

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task FindBestHospital_ShouldPickClosestHospitalMatchingCapabilitiesAndCapacity()
    {
        await SeedHospitalsAsync();

        var req = new HospitalRequirement
        {
            Required = true,
            RequiredCapacity = 1,
            RequiredCapabilities = new List<HospitalCapability> { HospitalCapability.ICU }
        };

        // Case is close to South Hospital
        double caseLat = 40.7100;
        double caseLng = -74.0000;

        // Act
        var (selected, rejections) = await _matcher.FindBestHospitalAsync(req, caseLat, caseLng);

        // Assert
        Assert.NotNull(selected);
        Assert.Equal("South Hospital", selected.Name);
    }

    [Fact]
    public async Task FindBestHospital_ShouldSkipHospitalWithNoRemainingCapacity()
    {
        await SeedHospitalsAsync();

        // Let's ask for 2 ICU beds. South Hospital only has 1 left (Capacity 5, Load 4). North Hospital has 4 left (Capacity 5, Load 1).
        var req = new HospitalRequirement
        {
            Required = true,
            RequiredCapacity = 2,
            RequiredCapabilities = new List<HospitalCapability> { HospitalCapability.ICU }
        };

        // Case is close to South Hospital
        double caseLat = 40.7100;
        double caseLng = -74.0000;

        // Act
        var (selected, rejections) = await _matcher.FindBestHospitalAsync(req, caseLat, caseLng);

        // Assert
        Assert.NotNull(selected);
        Assert.Equal("North Hospital", selected.Name); // South should be skipped due to capacity limits
        Assert.Contains(rejections, r => r.Contains("South Hospital has insufficient capacity"));
    }
}
