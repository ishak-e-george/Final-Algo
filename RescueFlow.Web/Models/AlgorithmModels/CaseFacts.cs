using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Models.AlgorithmModels;

public class CaseFacts
{
    public IncidentCategory IncidentCategory { get; set; }
    public InjuryType InjuryType { get; set; }

    public int NumberOfPatients { get; set; }

    public bool HasSevereBleeding { get; set; }
    public bool HasBreathingProblem { get; set; }
    public bool IsUnconscious { get; set; }

    public bool HasFire { get; set; }
    public bool HasTrappedVictim { get; set; }
    public bool HasChemicalRisk { get; set; }
    public bool HasExplosionRisk { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string Description { get; set; } = "";
}
