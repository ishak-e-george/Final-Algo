using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Entities;
using RescueFlow.Web.Models.ViewModels;

namespace RescueFlow.Web.Services;

public class CaseFactsBuilder
{
    public CaseFacts Build(RescueCaseCreateViewModel model)
    {
        return new CaseFacts
        {
            IncidentCategory = model.IncidentCategory,
            InjuryType = model.InjuryType,
            NumberOfPatients = model.NumberOfPatients,
            HasSevereBleeding = model.HasSevereBleeding,
            HasBreathingProblem = model.HasBreathingProblem,
            IsUnconscious = model.IsUnconscious,
            HasFire = model.HasFire,
            HasTrappedVictim = model.HasTrappedVictim,
            HasChemicalRisk = model.HasChemicalRisk,
            HasExplosionRisk = model.HasExplosionRisk,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            Description = model.Description
        };
    }

    public CaseFacts Build(RescueCase entity)
    {
        return new CaseFacts
        {
            IncidentCategory = entity.IncidentCategory,
            InjuryType = entity.InjuryType,
            NumberOfPatients = entity.NumberOfPatients,
            HasSevereBleeding = entity.HasSevereBleeding,
            HasBreathingProblem = entity.HasBreathingProblem,
            IsUnconscious = entity.IsUnconscious,
            HasFire = entity.HasFire,
            HasTrappedVictim = entity.HasTrappedVictim,
            HasChemicalRisk = entity.HasChemicalRisk,
            HasExplosionRisk = entity.HasExplosionRisk,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            Description = entity.Description
        };
    }
}
