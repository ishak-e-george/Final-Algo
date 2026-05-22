using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Services;

public class CaseClassifier
{
    public CaseClassification Classify(CaseFacts facts)
    {
        var classification = new CaseClassification();

        // 1. Life Threatening checks
        if (facts.IsUnconscious || 
            facts.HasBreathingProblem || 
            facts.HasSevereBleeding ||
            facts.InjuryType == InjuryType.HeartAttack || 
            facts.InjuryType == InjuryType.Stroke || 
            facts.InjuryType == InjuryType.Trauma || 
            facts.InjuryType == InjuryType.Burn ||
            facts.InjuryType == InjuryType.ChemicalExposure || 
            facts.InjuryType == InjuryType.MultipleInjuries ||
            facts.NumberOfPatients >= 5)
        {
            classification.IsLifeThreatening = true;
            classification.ClassificationTags.Add("LifeThreatening");
        }

        // 2. Environmental Hazards
        if (facts.HasFire || facts.HasChemicalRisk || facts.HasExplosionRisk || facts.IncidentCategory == IncidentCategory.ChemicalHazard || facts.IncidentCategory == IncidentCategory.Explosion)
        {
            classification.HasEnvironmentalHazards = true;
            classification.ClassificationTags.Add("HazardousEnvironment");
        }

        // 3. Specialized Rescue
        if (facts.HasTrappedVictim || facts.HasChemicalRisk || facts.HasExplosionRisk || facts.IncidentCategory == IncidentCategory.TrappedVictim || facts.IncidentCategory == IncidentCategory.BuildingCollapse)
        {
            classification.RequiresSpecializedRescue = true;
            classification.ClassificationTags.Add("SpecializedRescue");
        }

        // Add more descriptive tags based on details
        if (facts.InjuryType == InjuryType.HeartAttack)
            classification.ClassificationTags.Add("CardiacEmergency");
        if (facts.InjuryType == InjuryType.Stroke)
            classification.ClassificationTags.Add("StrokeEmergency");
        if (facts.HasFire)
            classification.ClassificationTags.Add("FireActive");
        if (facts.HasTrappedVictim)
            classification.ClassificationTags.Add("Entrapment");
        if (facts.HasChemicalRisk)
            classification.ClassificationTags.Add("HazMat");
        if (facts.NumberOfPatients >= 5)
            classification.ClassificationTags.Add("MassCasualty");

        return classification;
    }
}
