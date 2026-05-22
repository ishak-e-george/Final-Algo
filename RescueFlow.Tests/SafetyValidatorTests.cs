using System;
using System.Collections.Generic;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Entities;
using RescueFlow.Web.Models.Enums;
using RescueFlow.Web.Services;
using Xunit;

namespace RescueFlow.Tests;

public class SafetyValidatorTests
{
    private readonly SafetyValidator _validator = new();

    [Fact]
    public void Validate_MissingRequiredVehicleCount_ShouldFail()
    {
        // Arrange
        var reqs = new List<ResourceRequirement>
        {
            new() { VehicleType = VehicleType.Ambulance, RequiredVehicleCount = 2 }
        };
        var selected = new List<RescueVehicle>
        {
            new() { Code = "AMB-01", VehicleType = VehicleType.Ambulance, Status = VehicleStatus.Available, CurrentCrewCount = 2 }
        };
        var hospReq = new HospitalRequirement { Required = false };

        // Act
        var (isValid, violations) = _validator.Validate(reqs, selected, hospReq, null);

        // Assert
        Assert.False(isValid);
        Assert.Contains(violations, v => v.Contains("Missing required vehicle count"));
    }

    [Fact]
    public void Validate_VehicleNotAvailable_ShouldFail()
    {
        // Arrange
        var reqs = new List<ResourceRequirement>
        {
            new() { VehicleType = VehicleType.Ambulance, RequiredVehicleCount = 1 }
        };
        var selected = new List<RescueVehicle>
        {
            new() { Code = "AMB-01", VehicleType = VehicleType.Ambulance, Status = VehicleStatus.Maintenance, CurrentCrewCount = 2 }
        };
        var hospReq = new HospitalRequirement { Required = false };

        // Act
        var (isValid, violations) = _validator.Validate(reqs, selected, hospReq, null);

        // Assert
        Assert.False(isValid);
        Assert.Contains(violations, v => v.Contains("must be 'Available'"));
    }

    [Fact]
    public void Validate_VehicleInsufficientCrew_ShouldFail()
    {
        // Arrange
        var reqs = new List<ResourceRequirement>
        {
            new() { VehicleType = VehicleType.Ambulance, RequiredVehicleCount = 1, RequiredPersonnelPerVehicle = 3 }
        };
        var selected = new List<RescueVehicle>
        {
            new() { Code = "AMB-01", VehicleType = VehicleType.Ambulance, Status = VehicleStatus.Available, CurrentCrewCount = 2 }
        };
        var hospReq = new HospitalRequirement { Required = false };

        // Act
        var (isValid, violations) = _validator.Validate(reqs, selected, hospReq, null);

        // Assert
        Assert.False(isValid);
        Assert.Contains(violations, v => v.Contains("insufficient crew count"));
    }

    [Fact]
    public void Validate_VehicleMissingEquipment_ShouldFail()
    {
        // Arrange
        var reqs = new List<ResourceRequirement>
        {
            new() { VehicleType = VehicleType.Ambulance, RequiredVehicleCount = 1, RequiredEquipment = new List<string> { "Trauma Bag", "Defibrillator" } }
        };
        var selected = new List<RescueVehicle>
        {
            new()
            {
                Code = "AMB-01",
                VehicleType = VehicleType.Ambulance,
                Status = VehicleStatus.Available,
                CurrentCrewCount = 2,
                VehicleEquipment = new List<VehicleEquipment>
                {
                    new() { Equipment = new Equipment { Name = "Trauma Bag" } }
                }
            }
        };
        var hospReq = new HospitalRequirement { Required = false };

        // Act
        var (isValid, violations) = _validator.Validate(reqs, selected, hospReq, null);

        // Assert
        Assert.False(isValid);
        Assert.Contains(violations, v => v.Contains("missing required equipment: Defibrillator"));
    }

    [Fact]
    public void Validate_HospitalMissingCapability_ShouldFail()
    {
        // Arrange
        var reqs = new List<ResourceRequirement>();
        var selected = new List<RescueVehicle>();
        var hospReq = new HospitalRequirement
        {
            Required = true,
            RequiredCapacity = 1,
            RequiredCapabilities = new List<HospitalCapability> { HospitalCapability.ICU }
        };
        var hospital = new Hospital
        {
            Name = "City Clinic",
            Capabilities = new List<HospitalCapabilityEntity>
            {
                new() { Capability = HospitalCapability.GeneralEmergency }
            },
            Capacity = new HospitalCapacity { IcuCapacity = 2, CurrentIcuLoad = 0 }
        };

        // Act
        var (isValid, violations) = _validator.Validate(reqs, selected, hospReq, hospital);

        // Assert
        Assert.False(isValid);
        Assert.Contains(violations, v => v.Contains("missing required capabilities: ICU"));
    }

    [Fact]
    public void Validate_HospitalInsufficientCapacity_ShouldFail()
    {
        // Arrange
        var reqs = new List<ResourceRequirement>();
        var selected = new List<RescueVehicle>();
        var hospReq = new HospitalRequirement
        {
            Required = true,
            RequiredCapacity = 2,
            RequiredCapabilities = new List<HospitalCapability> { HospitalCapability.GeneralEmergency }
        };
        var hospital = new Hospital
        {
            Name = "City Clinic",
            Capabilities = new List<HospitalCapabilityEntity>
            {
                new() { Capability = HospitalCapability.GeneralEmergency }
            },
            Capacity = new HospitalCapacity { EmergencyCapacity = 2, CurrentEmergencyLoad = 1 } // Only 1 slot left, need 2
        };

        // Act
        var (isValid, violations) = _validator.Validate(reqs, selected, hospReq, hospital);

        // Assert
        Assert.False(isValid);
        Assert.Contains(violations, v => v.Contains("does not have sufficient capacity"));
    }
}
