using System;
using System.Collections.Generic;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Services;

public class RequirementEngine
{
    public List<ResourceRequirement> GenerateVehicleRequirements(CaseFacts facts, CaseClassification classification, int severity)
    {
        var requirements = new List<ResourceRequirement>();

        // Ambulance requirement if there are patients/injuries
        if (facts.InjuryType != InjuryType.None || facts.NumberOfPatients > 0)
        {
            requirements.Add(new ResourceRequirement
            {
                VehicleType = VehicleType.Ambulance,
                RequiredVehicleCount = severity >= 5 ? 3 : 1,
                RequiredPersonnelPerVehicle = 2,
                RequiredEquipment = new List<string> { "Trauma Bag" }
            });
        }

        // Fire active or fire threat requires FireEngine
        if (facts.HasFire || classification.ClassificationTags.Contains("FireActive"))
        {
            requirements.Add(new ResourceRequirement
            {
                VehicleType = VehicleType.FireEngine,
                RequiredVehicleCount = severity >= 5 ? 2 : 1,
                RequiredPersonnelPerVehicle = 3,
                RequiredEquipment = new List<string> { "Fire Hose", "Water Pump" }
            });
        }

        // Trapped victims require Heavy Rescue
        if (facts.HasTrappedVictim || classification.ClassificationTags.Contains("Entrapment"))
        {
            requirements.Add(new ResourceRequirement
            {
                VehicleType = VehicleType.HeavyRescueVehicle,
                RequiredVehicleCount = 1,
                RequiredPersonnelPerVehicle = 4,
                RequiredEquipment = new List<string>
                {
                    "Hydraulic Cutter",
                    "Stabilization Struts"
                }
            });
        }

        // Chemical risk requires HazMat
        if (facts.HasChemicalRisk || classification.ClassificationTags.Contains("HazMat"))
        {
            requirements.Add(new ResourceRequirement
            {
                VehicleType = VehicleType.HazMatVehicle,
                RequiredVehicleCount = 1,
                RequiredPersonnelPerVehicle = 3,
                RequiredEquipment = new List<string>
                {
                    "Gas Detector",
                    "Chemical Suit"
                }
            });
        }

        return requirements;
    }

    public HospitalRequirement GenerateHospitalRequirement(CaseFacts facts, CaseClassification classification, int severity)
    {
        var requirement = new HospitalRequirement();

        if (severity < 3 && facts.InjuryType == InjuryType.MinorInjury && !classification.IsLifeThreatening)
        {
            requirement.Required = false;
            return requirement;
        }

        requirement.Required = true;
        requirement.RequiredCapacity = Math.Max(1, facts.NumberOfPatients);

        switch (facts.InjuryType)
        {
            case InjuryType.Trauma:
            case InjuryType.MultipleInjuries:
                requirement.RequiredCapabilities.Add(HospitalCapability.Trauma);
                break;

            case InjuryType.Burn:
                requirement.RequiredCapabilities.Add(HospitalCapability.BurnUnit);
                break;

            case InjuryType.HeartAttack:
                requirement.RequiredCapabilities.Add(HospitalCapability.Cardiac);
                requirement.RequiredCapabilities.Add(HospitalCapability.ICU);
                break;

            case InjuryType.Stroke:
                requirement.RequiredCapabilities.Add(HospitalCapability.Stroke);
                requirement.RequiredCapabilities.Add(HospitalCapability.ICU);
                break;

            case InjuryType.ChemicalExposure:
            case InjuryType.Poisoning:
                requirement.RequiredCapabilities.Add(HospitalCapability.Toxicology);
                requirement.RequiredCapabilities.Add(HospitalCapability.ICU);
                break;

            default:
                requirement.RequiredCapabilities.Add(HospitalCapability.GeneralEmergency);
                break;
        }

        return requirement;
    }
}
