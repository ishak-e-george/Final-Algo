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

public class EndToEndDispatchTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RescueFlowDbContext _context;
    private readonly CaseFactsBuilder _factsBuilder;
    private readonly CaseClassifier _classifier;
    private readonly SeverityAnalyzer _severityAnalyzer;
    private readonly RequirementEngine _requirementEngine;
    private readonly ResourceMatcher _resourceMatcher;
    private readonly HospitalMatcher _hospitalMatcher;
    private readonly ResponsePlanGenerator _planGenerator;
    private readonly SafetyValidator _safetyValidator;
    private readonly AutoAssignmentService _assignmentService;

    public class MockRoutingService : IRoutingService
    {
        public Task<RouteEstimate> EstimateAsync(Location from, Location to)
        {
            return Task.FromResult(new RouteEstimate(5.0, 10.0));
        }
    }

    public EndToEndDispatchTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<RescueFlowDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new RescueFlowDbContext(options);
        _context.Database.EnsureCreated();

        var routing = new MockRoutingService();
        _factsBuilder = new CaseFactsBuilder();
        _classifier = new CaseClassifier();
        _severityAnalyzer = new SeverityAnalyzer();
        _requirementEngine = new RequirementEngine();
        _resourceMatcher = new ResourceMatcher(_context, routing);
        _hospitalMatcher = new HospitalMatcher(_context, routing);
        _planGenerator = new ResponsePlanGenerator(routing);
        _safetyValidator = new SafetyValidator();
        _assignmentService = new AutoAssignmentService(_context, _safetyValidator);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task SeedDatabaseAsync()
    {
        // 1. Station
        var station = new Station { Id = 1, Name = "HQ Station", Latitude = 40.0, Longitude = -74.0, IsActive = true };
        _context.Stations.Add(station);

        // 2. Equipment
        var equip1 = new Equipment { Id = 1, Name = "Trauma Bag" };
        var equip2 = new Equipment { Id = 2, Name = "Hydraulic Cutter" };
        var equip3 = new Equipment { Id = 3, Name = "Stabilization Struts" };
        _context.Equipment.AddRange(equip1, equip2, equip3);
        await _context.SaveChangesAsync();

        // 3. Vehicles
        var amb = new RescueVehicle
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

        var rescue = new RescueVehicle
        {
            Id = 2,
            Code = "HR-01",
            StationId = 1,
            VehicleType = VehicleType.HeavyRescueVehicle,
            Status = VehicleStatus.Available,
            CurrentCrewCount = 4,
            MaxCrewCapacity = 4,
            CurrentLatitude = 40.0,
            CurrentLongitude = -74.0
        };

        _context.RescueVehicles.AddRange(amb, rescue);
        await _context.SaveChangesAsync();

        _context.VehicleEquipment.AddRange(
            new VehicleEquipment { RescueVehicleId = 1, EquipmentId = 1 },
            new VehicleEquipment { RescueVehicleId = 2, EquipmentId = 2 },
            new VehicleEquipment { RescueVehicleId = 2, EquipmentId = 3 }
        );

        // 4. Hospital
        var hospital = new Hospital { Id = 1, Name = "Trauma Center", Latitude = 40.05, Longitude = -74.05, IsActive = true };
        _context.Hospitals.Add(hospital);
        await _context.SaveChangesAsync();

        _context.HospitalCapabilities.Add(new HospitalCapabilityEntity { HospitalId = 1, Capability = HospitalCapability.Trauma });
        _context.HospitalCapacities.Add(new HospitalCapacity
        {
            HospitalId = 1,
            TraumaCapacity = 10,
            CurrentTraumaLoad = 0
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task FullPipeline_TrappedVictimCase_ShouldSucceedAndPersistResponsePlan()
    {
        await SeedDatabaseAsync();

        // 1. Create RescueCase
        var rescueCase = new RescueCase
        {
            Id = 1,
            ReporterName = "Caller John",
            ReporterPhone = "555-1234",
            LocationDescription = "Highway intersection",
            Latitude = 40.01,
            Longitude = -74.01,
            IncidentCategory = IncidentCategory.RoadAccident,
            InjuryType = InjuryType.MultipleInjuries,
            NumberOfPatients = 1,
            HasTrappedVictim = true,
            HasSevereBleeding = true,
            Status = CaseStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.RescueCases.Add(rescueCase);
        await _context.SaveChangesAsync();

        // 2. Build CaseFacts & Classify
        var facts = _factsBuilder.Build(rescueCase);
        var classification = _classifier.Classify(facts);

        // 3. Analyze severity
        int severity = _severityAnalyzer.CalculateSeverity(facts);
        rescueCase.CalculatedSeverity = severity;
        await _context.SaveChangesAsync();

        Assert.Equal(4, severity); // Severity should be 4 due to trapped victim / severe bleeding

        // 4. Generate requirements
        var vehicleRequirements = _requirementEngine.GenerateVehicleRequirements(facts, classification, severity);
        var hospitalRequirement = _requirementEngine.GenerateHospitalRequirement(facts, classification, severity);

        Assert.Contains(vehicleRequirements, r => r.VehicleType == VehicleType.Ambulance);
        Assert.Contains(vehicleRequirements, r => r.VehicleType == VehicleType.HeavyRescueVehicle);
        Assert.True(hospitalRequirement.Required);
        Assert.Contains(HospitalCapability.Trauma, hospitalRequirement.RequiredCapabilities);

        // 5. Match vehicles
        var (selectedVehicles, rejectedVehicles) = await _resourceMatcher.FindEligibleVehiclesAsync(
            vehicleRequirements, rescueCase.Latitude, rescueCase.Longitude);

        Assert.Equal(2, selectedVehicles.Count);
        Assert.Contains(selectedVehicles, v => v.VehicleType == VehicleType.Ambulance);
        Assert.Contains(selectedVehicles, v => v.VehicleType == VehicleType.HeavyRescueVehicle);

        // 6. Match hospital
        var (selectedHospital, rejectedHospitals) = await _hospitalMatcher.FindBestHospitalAsync(
            hospitalRequirement, rescueCase.Latitude, rescueCase.Longitude);

        Assert.NotNull(selectedHospital);
        Assert.Equal("Trauma Center", selectedHospital.Name);

        // 7. Validate safety pre-reservation
        var (preValid, preViolations) = _safetyValidator.Validate(
            vehicleRequirements, selectedVehicles, hospitalRequirement, selectedHospital);
        
        Assert.True(preValid);
        Assert.Empty(preViolations);

        // 8. Generate response plan
        var plan = await _planGenerator.GeneratePlanAsync(
            rescueCase,
            selectedVehicles,
            selectedHospital,
            rescueCase.CalculatedSeverity,
            vehicleRequirements,
            hospitalRequirement,
            rejectedVehicles,
            rejectedHospitals
        );

        // 9. Auto assign and commit
        var committedPlan = await _assignmentService.AutoAssignAsync(
            rescueCase,
            plan,
            selectedVehicles,
            selectedHospital,
            vehicleRequirements,
            hospitalRequirement
        );

        // Assertions after transaction commits
        Assert.True(committedPlan.SafetyValidationPassed);
        Assert.Equal(CaseStatus.AutoAssigned, rescueCase.Status);

        // Verify that vehicles statuses are updated to Assigned in DB
        var dbAmb = await _context.RescueVehicles.AsNoTracking().FirstAsync(v => v.Id == 1);
        var dbRescue = await _context.RescueVehicles.AsNoTracking().FirstAsync(v => v.Id == 2);
        Assert.Equal(VehicleStatus.Assigned, dbAmb.Status);
        Assert.Equal(VehicleStatus.Assigned, dbRescue.Status);

        // Verify hospital capacity load is incremented in DB
        var dbCapacity = await _context.HospitalCapacities.AsNoTracking().FirstAsync(c => c.HospitalId == 1);
        Assert.Equal(1, dbCapacity.CurrentTraumaLoad);

        // Verify ResponsePlan exists in DB
        var dbPlan = await _context.ResponsePlans
            .Include(p => p.Assignments)
            .FirstOrDefaultAsync(p => p.RescueCaseId == rescueCase.Id);

        Assert.NotNull(dbPlan);
        Assert.Equal(2, dbPlan.Assignments.Count);
        Assert.True(dbPlan.SafetyValidationPassed);
        Assert.Equal("Trauma Center", selectedHospital.Name);
    }
}
