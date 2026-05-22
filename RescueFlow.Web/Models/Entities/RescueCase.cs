using System;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Models.Entities;

public class RescueCase
{
    public int Id { get; set; }

    public string ReporterName { get; set; } = "";
    public string ReporterPhone { get; set; } = "";

    public string LocationDescription { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }

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

    public string Description { get; set; } = "";

    public int CalculatedSeverity { get; set; }
    public CaseStatus Status { get; set; } = CaseStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ResponsePlan? ResponsePlan { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
