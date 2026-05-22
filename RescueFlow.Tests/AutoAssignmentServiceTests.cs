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

public class AutoAssignmentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RescueFlowDbContext _context;
    private readonly SafetyValidator _validator;
    private readonly AutoAssignmentService _service;

    public AutoAssignmentServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<RescueFlowDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new RescueFlowDbContext(options);
        _context.Database.EnsureCreated();

        _validator = new SafetyValidator();
        _service = new AutoAssignmentService(_context, _validator);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task SeedDatabaseAsync()
    {
        // Stations
        var station = new Station { Id = 1, Name = "Main Station", Latitude = 40.0, Longitude = -74.0, IsActive = true };
        _context.Stations.Add(station);

        // Equipment
        var equip = new Equipment { Id = 1, Name = "Trauma Bag" };
        _context.Equipment.Add(equip);
        await _context.SaveChangesAsync();

        // Vehicles
        var vehicle = new RescueVehicle
        {
            Id = 1,
            Code = "AMB-01",
            StationId = 1,
            VehicleType = VehicleType.Ambulance,
            Status = VehicleStatus.Available,
            CurrentCrewCount = 2,
            MaxCrewCapacity = 2,
            CurrentLatitude = 40.0,
            CurrentLongitude = -74.0
        };
        _context.RescueVehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        _context.VehicleEquipment.Add(new VehicleEquipment { RescueVehicleId = 1, EquipmentId = 1 });

        // Hospital
        var hospital = new Hospital { Id = 1, Name = "City Hospital", Latitude = 40.1, Longitude = -74.1, IsActive = true };
        _context.Hospitals.Add(hospital);
        await _context.SaveChangesAsync();

        _context.HospitalCapabilities.Add(new HospitalCapabilityEntity { HospitalId = 1, Capability = HospitalCapability.ICU });
        _context.HospitalCapacities.Add(new HospitalCapacity
        {
            HospitalId = 1,
            IcuCapacity = 1, // Only 1 ICU bed
            CurrentIcuLoad = 0
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task AutoAssign_SuccessfulReservation_ShouldCommitAndChangeStatuses()
    {
        await SeedDatabaseAsync();

        var rescueCase = new RescueCase
        {
            Id = 1,
            IncidentCategory = IncidentCategory.Medical,
            InjuryType = InjuryType.HeartAttack,
            NumberOfPatients = 1,
            Latitude = 40.0,
            Longitude = -74.0,
            Status = CaseStatus.Pending
        };
        _context.RescueCases.Add(rescueCase);
        await _context.SaveChangesAsync();

        var vehicle = await _context.RescueVehicles.Include(v => v.VehicleEquipment).ThenInclude(ve => ve.Equipment).FirstAsync(v => v.Id == 1);
        var hospital = await _context.Hospitals.Include(h => h.Capabilities).Include(h => h.Capacity).FirstAsync(h => h.Id == 1);

        var vehicleReqs = new List<ResourceRequirement>
        {
            new() { VehicleType = VehicleType.Ambulance, RequiredVehicleCount = 1, RequiredPersonnelPerVehicle = 2, RequiredEquipment = new List<string> { "Trauma Bag" } }
        };
        var hospitalReq = new HospitalRequirement
        {
            Required = true,
            RequiredCapacity = 1,
            RequiredCapabilities = new List<HospitalCapability> { HospitalCapability.ICU }
        };

        var responsePlan = new ResponsePlan
        {
            RescueCaseId = rescueCase.Id,
            RequiresHospital = true,
            SelectedHospitalId = hospital.Id,
            SeverityLevel = 3,
            CreatedAt = DateTime.UtcNow,
            Assignments = new List<ResponseAssignment>
            {
                new() { RescueVehicleId = vehicle.Id, IsActive = true, AssignedAtUtc = DateTime.UtcNow }
            }
        };

        // Act
        var plan = await _service.AutoAssignAsync(rescueCase, responsePlan, new List<RescueVehicle> { vehicle }, hospital, vehicleReqs, hospitalReq);

        // Assert
        Assert.True(plan.SafetyValidationPassed);
        Assert.Equal(CaseStatus.AutoAssigned, rescueCase.Status);

        var dbVehicle = await _context.RescueVehicles.AsNoTracking().FirstAsync(v => v.Id == 1);
        Assert.Equal(VehicleStatus.Assigned, dbVehicle.Status);

        var dbCapacity = await _context.HospitalCapacities.AsNoTracking().FirstAsync(c => c.HospitalId == 1);
        Assert.Equal(1, dbCapacity.CurrentIcuLoad);
    }

    [Fact]
    public async Task AutoAssign_RollbackOnHospitalFailure_ShouldKeepVehicleAvailable()
    {
        await SeedDatabaseAsync();

        // Let's pre-fill the hospital capacity so hospital reservation fails.
        var dbCapacity = await _context.HospitalCapacities.FirstAsync(c => c.HospitalId == 1);
        dbCapacity.CurrentIcuLoad = 1; // 1 out of 1 bed is taken
        await _context.SaveChangesAsync();

        var rescueCase = new RescueCase
        {
            Id = 2,
            IncidentCategory = IncidentCategory.Medical,
            InjuryType = InjuryType.HeartAttack,
            NumberOfPatients = 1,
            Latitude = 40.0,
            Longitude = -74.0,
            Status = CaseStatus.Pending
        };
        _context.RescueCases.Add(rescueCase);
        await _context.SaveChangesAsync();

        var vehicle = await _context.RescueVehicles.Include(v => v.VehicleEquipment).ThenInclude(ve => ve.Equipment).FirstAsync(v => v.Id == 1);
        var hospital = await _context.Hospitals.Include(h => h.Capabilities).Include(h => h.Capacity).FirstAsync(h => h.Id == 1);

        var vehicleReqs = new List<ResourceRequirement>
        {
            new() { VehicleType = VehicleType.Ambulance, RequiredVehicleCount = 1, RequiredPersonnelPerVehicle = 2, RequiredEquipment = new List<string> { "Trauma Bag" } }
        };
        var hospitalReq = new HospitalRequirement
        {
            Required = true,
            RequiredCapacity = 1,
            RequiredCapabilities = new List<HospitalCapability> { HospitalCapability.ICU }
        };

        var responsePlan = new ResponsePlan
        {
            RescueCaseId = rescueCase.Id,
            RequiresHospital = true,
            SelectedHospitalId = hospital.Id,
            SeverityLevel = 3,
            CreatedAt = DateTime.UtcNow,
            Assignments = new List<ResponseAssignment>
            {
                new() { RescueVehicleId = vehicle.Id, IsActive = true, AssignedAtUtc = DateTime.UtcNow }
            }
        };

        // Act & Assert
        // This should throw because hospital capacity reservation fails.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AutoAssignAsync(rescueCase, responsePlan, new List<RescueVehicle> { vehicle }, hospital, vehicleReqs, hospitalReq)
        );

        // Verify that the vehicle remains "Available" (was rolled back)
        var freshVehicle = await _context.RescueVehicles.AsNoTracking().FirstAsync(v => v.Id == 1);
        Assert.Equal(VehicleStatus.Available, freshVehicle.Status);

        // Verify no active assignments exist in the database for the case
        var assignmentsCount = await _context.ResponseAssignments.CountAsync();
        Assert.Equal(0, assignmentsCount);
    }

    [Fact]
    public async Task DoubleBookingProtection_FilteredIndex_ShouldPreventTwoActiveAssignments()
    {
        await SeedDatabaseAsync();

        // Create a valid rescue case and response plan
        var rescueCase = new RescueCase
        {
            IncidentCategory = IncidentCategory.Medical,
            InjuryType = InjuryType.HeartAttack,
            NumberOfPatients = 1,
            Latitude = 40.0,
            Longitude = -74.0,
            Status = CaseStatus.Pending
        };
        _context.RescueCases.Add(rescueCase);
        await _context.SaveChangesAsync();

        var responsePlan = new ResponsePlan
        {
            RescueCaseId = rescueCase.Id,
            SeverityLevel = 3,
            CreatedAt = DateTime.UtcNow
        };
        _context.ResponsePlans.Add(responsePlan);
        await _context.SaveChangesAsync();

        // Add first active assignment for vehicle 1
        var assign1 = new ResponseAssignment
        {
            ResponsePlanId = responsePlan.Id,
            RescueVehicleId = 1,
            IsActive = true,
            AssignedAtUtc = DateTime.UtcNow
        };
        _context.ResponseAssignments.Add(assign1);
        await _context.SaveChangesAsync();

        // Adding a second active assignment for vehicle 1 should throw DbUpdateException due to filtered unique index in DB
        var assign2 = new ResponseAssignment
        {
            ResponsePlanId = responsePlan.Id,
            RescueVehicleId = 1,
            IsActive = true,
            AssignedAtUtc = DateTime.UtcNow
        };
        _context.ResponseAssignments.Add(assign2);

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task CloseCase_ShouldReleaseCapacityAndEnsureItNeverGoesBelowZero()
    {
        await SeedDatabaseAsync();

        // Let's create an active case, response plan, and assignments
        var rescueCase = new RescueCase
        {
            Id = 5,
            IncidentCategory = IncidentCategory.Medical,
            InjuryType = InjuryType.HeartAttack,
            NumberOfPatients = 1,
            Latitude = 40.0,
            Longitude = -74.0,
            Status = CaseStatus.AutoAssigned
        };
        _context.RescueCases.Add(rescueCase);

        // Put vehicle to Assigned
        var vehicle = await _context.RescueVehicles.FirstAsync(v => v.Id == 1);
        vehicle.Status = VehicleStatus.Assigned;

        // Put hospital load to 0 (underflow risk test if released capacity is greater than load)
        var capacity = await _context.HospitalCapacities.FirstAsync(c => c.HospitalId == 1);
        capacity.CurrentIcuLoad = 0; // Load is 0 in DB right now!

        var responsePlan = new ResponsePlan
        {
            Id = 5,
            RescueCaseId = 5,
            RequiresHospital = true,
            SelectedHospitalId = 1,
            SeverityLevel = 3,
            CreatedAt = DateTime.UtcNow,
            Assignments = new List<ResponseAssignment>
            {
                new() { RescueVehicleId = 1, IsActive = true, AssignedAtUtc = DateTime.UtcNow }
            }
        };
        _context.ResponsePlans.Add(responsePlan);
        await _context.SaveChangesAsync();

        // Execute Case Closure simulation (analogous to RescueCasesController.Close method)
        using var transaction = await _context.Database.BeginTransactionAsync();
        rescueCase.Status = CaseStatus.Closed;

        // Release vehicles
        var vehicleIds = responsePlan.Assignments
            .Where(a => a.IsActive)
            .Select(a => a.RescueVehicleId)
            .ToList();

        await _context.RescueVehicles
            .Where(v => vehicleIds.Contains(v.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Status, VehicleStatus.Available));

        await _context.ResponseAssignments
            .Where(a => a.ResponsePlanId == responsePlan.Id && a.IsActive)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsActive, false)
                .SetProperty(a => a.ReleasedAtUtc, DateTime.UtcNow)
            );

        // Release hospital capacity - required capacity was 1 ICU bed
        // Set load: c.CurrentIcuLoad - 1. If it goes below zero, set to 0.
        await _context.HospitalCapacities
            .Where(c => c.HospitalId == 1)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.CurrentIcuLoad, c => (c.CurrentIcuLoad - 1 > 0 ? c.CurrentIcuLoad - 1 : 0))
            );

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Assert
        var freshVehicle = await _context.RescueVehicles.AsNoTracking().FirstAsync(v => v.Id == 1);
        Assert.Equal(VehicleStatus.Available, freshVehicle.Status);

        var freshCapacity = await _context.HospitalCapacities.AsNoTracking().FirstAsync(c => c.HospitalId == 1);
        Assert.Equal(0, freshCapacity.CurrentIcuLoad); // Confirmed: load stayed at 0, did not go to -1
    }
}
