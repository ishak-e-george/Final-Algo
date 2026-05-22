using System;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Services;

public class SeverityAnalyzer
{
    public int CalculateSeverity(CaseFacts facts)
    {
        int severity = 1;

        if (facts.InjuryType == InjuryType.MinorInjury)
            severity = Math.Max(severity, 1);

        if (facts.InjuryType == InjuryType.Fracture || facts.InjuryType == InjuryType.Bleeding)
            severity = Math.Max(severity, 2);

        if (facts.InjuryType == InjuryType.Trauma ||
            facts.InjuryType == InjuryType.Burn ||
            facts.InjuryType == InjuryType.Stroke ||
            facts.InjuryType == InjuryType.HeartAttack)
            severity = Math.Max(severity, 3);

        if (facts.HasSevereBleeding)
            severity = Math.Max(severity, 4);

        if (facts.HasBreathingProblem)
            severity = Math.Max(severity, 4);

        if (facts.IsUnconscious)
            severity = Math.Max(severity, 4);

        if (facts.HasTrappedVictim)
            severity = Math.Max(severity, 4);

        if (facts.HasChemicalRisk)
            severity = Math.Max(severity, 4);

        if (facts.HasExplosionRisk)
            severity = Math.Max(severity, 5);

        if (facts.NumberOfPatients >= 5)
            severity = Math.Max(severity, 5);

        return Math.Clamp(severity, 1, 5);
    }
}
