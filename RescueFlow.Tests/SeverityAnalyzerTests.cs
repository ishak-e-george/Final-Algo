using System;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Enums;
using RescueFlow.Web.Services;
using Xunit;

namespace RescueFlow.Tests;

public class SeverityAnalyzerTests
{
    private readonly SeverityAnalyzer _analyzer = new();

    [Theory]
    [InlineData(InjuryType.MinorInjury, 1)]
    [InlineData(InjuryType.Fracture, 2)]
    [InlineData(InjuryType.Bleeding, 2)]
    [InlineData(InjuryType.Trauma, 3)]
    [InlineData(InjuryType.Burn, 3)]
    [InlineData(InjuryType.Stroke, 3)]
    [InlineData(InjuryType.HeartAttack, 3)]
    public void CalculateSeverity_BasedOnInjuryType_ShouldSetCorrectBaseSeverity(InjuryType injuryType, int expectedSeverity)
    {
        // Arrange
        var facts = new CaseFacts
        {
            InjuryType = injuryType,
            NumberOfPatients = 1
        };

        // Act
        int severity = _analyzer.CalculateSeverity(facts);

        // Assert
        Assert.Equal(expectedSeverity, severity);
    }

    [Fact]
    public void CalculateSeverity_MinorInjuryWithLifeThreats_ShouldElevateSeverityToFour()
    {
        // Arrange
        var factsWithBleeding = new CaseFacts { InjuryType = InjuryType.MinorInjury, HasSevereBleeding = true, NumberOfPatients = 1 };
        var factsWithBreathing = new CaseFacts { InjuryType = InjuryType.MinorInjury, HasBreathingProblem = true, NumberOfPatients = 1 };
        var factsUnconscious = new CaseFacts { InjuryType = InjuryType.MinorInjury, IsUnconscious = true, NumberOfPatients = 1 };
        var factsTrapped = new CaseFacts { InjuryType = InjuryType.MinorInjury, HasTrappedVictim = true, NumberOfPatients = 1 };
        var factsChemical = new CaseFacts { InjuryType = InjuryType.MinorInjury, HasChemicalRisk = true, NumberOfPatients = 1 };

        // Act & Assert
        Assert.Equal(4, _analyzer.CalculateSeverity(factsWithBleeding));
        Assert.Equal(4, _analyzer.CalculateSeverity(factsWithBreathing));
        Assert.Equal(4, _analyzer.CalculateSeverity(factsUnconscious));
        Assert.Equal(4, _analyzer.CalculateSeverity(factsTrapped));
        Assert.Equal(4, _analyzer.CalculateSeverity(factsChemical));
    }

    [Fact]
    public void CalculateSeverity_ExplosionRiskOrHighPatientCount_ShouldElevateSeverityToFive()
    {
        // Arrange
        var factsExplosion = new CaseFacts { InjuryType = InjuryType.MinorInjury, HasExplosionRisk = true, NumberOfPatients = 1 };
        var factsHighPatients = new CaseFacts { InjuryType = InjuryType.MinorInjury, NumberOfPatients = 5 };

        // Act & Assert
        Assert.Equal(5, _analyzer.CalculateSeverity(factsExplosion));
        Assert.Equal(5, _analyzer.CalculateSeverity(factsHighPatients));
    }

    [Fact]
    public void CalculateSeverity_ShouldClampBetweenOneAndFive()
    {
        // Arrange
        var factsZeroPatients = new CaseFacts { InjuryType = InjuryType.None, NumberOfPatients = 0 };
        var factsMassivePatients = new CaseFacts { InjuryType = InjuryType.Trauma, NumberOfPatients = 100, HasExplosionRisk = true };

        // Act & Assert
        Assert.Equal(1, _analyzer.CalculateSeverity(factsZeroPatients));
        Assert.Equal(5, _analyzer.CalculateSeverity(factsMassivePatients));
    }
}
