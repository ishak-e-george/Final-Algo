using System;
using System.Collections.Generic;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Enums;
using RescueFlow.Web.Services;
using Xunit;

namespace RescueFlow.Tests;

public class RequirementEngineTests
{
    private readonly RequirementEngine _engine = new();

    [Fact]
    public void MinorInjury_RequiresOneAmbulanceAndNoHospital()
    {
        // Arrange
        var facts = new CaseFacts
        {
            InjuryType = InjuryType.MinorInjury,
            NumberOfPatients = 1
        };
        var classification = new CaseClassification { IsLifeThreatening = false, ClassificationTags = new List<string>() };
        int severity = 1;

        // Act
        var vehicleReqs = _engine.GenerateVehicleRequirements(facts, classification, severity);
        var hospitalReq = _engine.GenerateHospitalRequirement(facts, classification, severity);

        // Assert
        Assert.Single(vehicleReqs);
        var ambReq = vehicleReqs[0];
        Assert.Equal(VehicleType.Ambulance, ambReq.VehicleType);
        Assert.Equal(1, ambReq.RequiredVehicleCount);
        Assert.Contains("Trauma Bag", ambReq.RequiredEquipment);

        Assert.False(hospitalReq.Required, "Minor injury should not require a hospital.");
    }

    [Fact]
    public void HeartAttack_RequiresAmbulanceAndIcuCardiacHospital()
    {
        // Arrange
        var facts = new CaseFacts
        {
            InjuryType = InjuryType.HeartAttack,
            NumberOfPatients = 1
        };
        var classification = new CaseClassification { IsLifeThreatening = true, ClassificationTags = new List<string> { "Medical" } };
        int severity = 3;

        // Act
        var vehicleReqs = _engine.GenerateVehicleRequirements(facts, classification, severity);
        var hospitalReq = _engine.GenerateHospitalRequirement(facts, classification, severity);

        // Assert
        Assert.Contains(vehicleReqs, r => r.VehicleType == VehicleType.Ambulance);
        var ambReq = vehicleReqs.Find(r => r.VehicleType == VehicleType.Ambulance);
        Assert.NotNull(ambReq);
        Assert.Equal(1, ambReq.RequiredVehicleCount);

        Assert.True(hospitalReq.Required);
        Assert.Equal(1, hospitalReq.RequiredCapacity);
        Assert.Contains(HospitalCapability.Cardiac, hospitalReq.RequiredCapabilities);
        Assert.Contains(HospitalCapability.ICU, hospitalReq.RequiredCapabilities);
    }

    [Fact]
    public void TrappedVictim_RequiresAmbulanceAndFireEngineAndHeavyRescue()
    {
        // Arrange
        var facts = new CaseFacts
        {
            InjuryType = InjuryType.MultipleInjuries,
            NumberOfPatients = 2,
            HasTrappedVictim = true,
            HasFire = true
        };
        var classification = new CaseClassification { IsLifeThreatening = true, ClassificationTags = new List<string> { "Entrapment", "FireActive" } };
        int severity = 4;

        // Act
        var vehicleReqs = _engine.GenerateVehicleRequirements(facts, classification, severity);
        var hospitalReq = _engine.GenerateHospitalRequirement(facts, classification, severity);

        // Assert
        Assert.Contains(vehicleReqs, r => r.VehicleType == VehicleType.Ambulance);
        Assert.Contains(vehicleReqs, r => r.VehicleType == VehicleType.FireEngine);
        Assert.Contains(vehicleReqs, r => r.VehicleType == VehicleType.HeavyRescueVehicle);

        var heavyRescueReq = vehicleReqs.Find(r => r.VehicleType == VehicleType.HeavyRescueVehicle);
        Assert.NotNull(heavyRescueReq);
        Assert.Contains("Hydraulic Cutter", heavyRescueReq.RequiredEquipment);
        Assert.Contains("Stabilization Struts", heavyRescueReq.RequiredEquipment);

        Assert.True(hospitalReq.Required);
        Assert.Contains(HospitalCapability.Trauma, hospitalReq.RequiredCapabilities);
    }

    [Fact]
    public void ChemicalHazard_RequiresAmbulanceAndFireEngineAndHazMat()
    {
        // Arrange
        var facts = new CaseFacts
        {
            InjuryType = InjuryType.ChemicalExposure,
            NumberOfPatients = 3,
            HasChemicalRisk = true,
            HasFire = true
        };
        var classification = new CaseClassification { IsLifeThreatening = true, ClassificationTags = new List<string> { "HazMat", "FireActive" } };
        int severity = 4;

        // Act
        var vehicleReqs = _engine.GenerateVehicleRequirements(facts, classification, severity);
        var hospitalReq = _engine.GenerateHospitalRequirement(facts, classification, severity);

        // Assert
        Assert.Contains(vehicleReqs, r => r.VehicleType == VehicleType.Ambulance);
        Assert.Contains(vehicleReqs, r => r.VehicleType == VehicleType.FireEngine);
        Assert.Contains(vehicleReqs, r => r.VehicleType == VehicleType.HazMatVehicle);

        var hazMatReq = vehicleReqs.Find(r => r.VehicleType == VehicleType.HazMatVehicle);
        Assert.NotNull(hazMatReq);
        Assert.Contains("Gas Detector", hazMatReq.RequiredEquipment);
        Assert.Contains("Chemical Suit", hazMatReq.RequiredEquipment);

        Assert.True(hospitalReq.Required);
        Assert.Contains(HospitalCapability.Toxicology, hospitalReq.RequiredCapabilities);
        Assert.Contains(HospitalCapability.ICU, hospitalReq.RequiredCapabilities);
    }
}
