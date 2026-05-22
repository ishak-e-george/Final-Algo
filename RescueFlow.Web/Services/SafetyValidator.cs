using System.Collections.Generic;
using System.Linq;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Entities;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Services;

public class SafetyValidator
{
    public (bool IsValid, List<string> Violations) Validate(
        List<ResourceRequirement> requirements,
        List<RescueVehicle> selectedVehicles,
        HospitalRequirement hospitalRequirement,
        Hospital? selectedHospital)
    {
        var violations = new List<string>();

        foreach (var req in requirements)
        {
            var vehiclesOfType = selectedVehicles
                .Where(v => v.VehicleType == req.VehicleType)
                .ToList();

            if (vehiclesOfType.Count < req.RequiredVehicleCount)
            {
                violations.Add($"Safety Violation: Missing required vehicle count for {req.VehicleType}. Needed: {req.RequiredVehicleCount}, Selected: {vehiclesOfType.Count}");
                continue;
            }

            foreach (var vehicle in vehiclesOfType)
            {
                if (vehicle.Status != VehicleStatus.Available)
                {
                    violations.Add($"Safety Violation: Vehicle {vehicle.Code} has status '{vehicle.Status}' but must be 'Available'");
                }

                if (vehicle.CurrentCrewCount < req.RequiredPersonnelPerVehicle)
                {
                    violations.Add($"Safety Violation: Vehicle {vehicle.Code} has insufficient crew count ({vehicle.CurrentCrewCount}). Required: {req.RequiredPersonnelPerVehicle}");
                }

                bool hasEquipment = req.RequiredEquipment.All(reqEquip =>
                    vehicle.VehicleEquipment.Any(ve => ve.Equipment.Name == reqEquip));

                if (!hasEquipment)
                {
                    var missing = req.RequiredEquipment
                        .Where(reqEquip => !vehicle.VehicleEquipment.Any(ve => ve.Equipment.Name == reqEquip))
                        .ToList();
                    violations.Add($"Safety Violation: Vehicle {vehicle.Code} is missing required equipment: {string.Join(", ", missing)}");
                }
            }
        }

        if (hospitalRequirement.Required)
        {
            if (selectedHospital == null)
            {
                violations.Add("Safety Violation: A hospital is required for this case, but none was selected.");
            }
            else
            {
                // Check capabilities
                bool hasCapabilities = hospitalRequirement.RequiredCapabilities.All(reqCap =>
                    selectedHospital.Capabilities.Any(hc => hc.Capability == reqCap));
                
                if (!hasCapabilities)
                {
                    violations.Add($"Safety Violation: Selected hospital {selectedHospital.Name} is missing required capabilities: {string.Join(", ", hospitalRequirement.RequiredCapabilities)}");
                }

                // Check capacity
                bool hasCapacity = HasCapacity(selectedHospital, hospitalRequirement);
                if (!hasCapacity)
                {
                    violations.Add($"Safety Violation: Selected hospital {selectedHospital.Name} does not have sufficient capacity ({hospitalRequirement.RequiredCapacity}) for requirements: {string.Join(", ", hospitalRequirement.RequiredCapabilities)}");
                }
            }
        }

        return (violations.Count == 0, violations);
    }

    private bool HasCapacity(Hospital hospital, HospitalRequirement requirement)
    {
        var capacity = hospital.Capacity;
        if (capacity == null) return false;

        foreach (var capability in requirement.RequiredCapabilities)
        {
            bool ok = capability switch
            {
                HospitalCapability.Trauma =>
                    capacity.TraumaCapacity - capacity.CurrentTraumaLoad >= requirement.RequiredCapacity,

                HospitalCapability.ICU =>
                    capacity.IcuCapacity - capacity.CurrentIcuLoad >= requirement.RequiredCapacity,

                HospitalCapability.BurnUnit =>
                    capacity.BurnCapacity - capacity.CurrentBurnLoad >= requirement.RequiredCapacity,

                HospitalCapability.Toxicology =>
                    capacity.ToxicologyCapacity - capacity.CurrentToxicologyLoad >= requirement.RequiredCapacity,

                HospitalCapability.GeneralEmergency =>
                    capacity.EmergencyCapacity - capacity.CurrentEmergencyLoad >= requirement.RequiredCapacity,

                _ => true
            };

            if (!ok)
                return false;
        }

        return true;
    }
}
