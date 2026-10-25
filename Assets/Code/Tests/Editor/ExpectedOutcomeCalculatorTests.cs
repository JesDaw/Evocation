#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;

public class ExpectedOutcomeCalculatorTests
{
    [Test]
    public void IdenticalArmies_ReturnTie()
    {
        var unit = new ExpectedOutcomeCalculator.CombatProfile(10f, 100f, 3f, 2f, 1);
        float outcome = ExpectedOutcomeCalculator.EvaluateProfiles(
            new List<ExpectedOutcomeCalculator.CombatProfile> { unit },
            new List<ExpectedOutcomeCalculator.CombatProfile> { unit });

        Assert.That(outcome, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void MoreUnits_GainNonlinearCombinedStrength()
    {
        var unit = new ExpectedOutcomeCalculator.CombatProfile(10f, 100f, 3f, 2f, 1);
        var aiSwarm = new List<ExpectedOutcomeCalculator.CombatProfile> { unit, unit, unit };
        var playerArmy = new List<ExpectedOutcomeCalculator.CombatProfile> { unit };

        Assert.That(ExpectedOutcomeCalculator.EvaluateProfiles(aiSwarm, playerArmy), Is.LessThan(0f));
    }

    [Test]
    public void Aoe_GainsEffectivenessAgainstMultipleTargets()
    {
        var singleTarget = new ExpectedOutcomeCalculator.CombatProfile(10f, 100f, 3f, 2f, 1);
        var aoe = new ExpectedOutcomeCalculator.CombatProfile(10f, 100f, 3f, 2f, 5);
        var aiSingle = new List<ExpectedOutcomeCalculator.CombatProfile> { singleTarget };
        var aiAoe = new List<ExpectedOutcomeCalculator.CombatProfile> { aoe };
        var playerSwarm = new List<ExpectedOutcomeCalculator.CombatProfile>
        {
            new ExpectedOutcomeCalculator.CombatProfile(2f, 20f, 1f, 2f, 1),
            new ExpectedOutcomeCalculator.CombatProfile(2f, 20f, 1f, 2f, 1),
            new ExpectedOutcomeCalculator.CombatProfile(2f, 20f, 1f, 2f, 1),
            new ExpectedOutcomeCalculator.CombatProfile(2f, 20f, 1f, 2f, 1)
        };

        float singleOutcome = ExpectedOutcomeCalculator.EvaluateProfiles(aiSingle, playerSwarm);
        float aoeOutcome = ExpectedOutcomeCalculator.EvaluateProfiles(aiAoe, playerSwarm);
        Assert.That(aoeOutcome, Is.LessThan(singleOutcome));
    }

    [Test]
    public void RangeAdvantage_ImprovesEstimatedOutcome()
    {
        var longRange = new ExpectedOutcomeCalculator.CombatProfile(10f, 100f, 10f, 2f, 1);
        var shortRange = new ExpectedOutcomeCalculator.CombatProfile(10f, 100f, 2f, 2f, 1);
        var opponent = new ExpectedOutcomeCalculator.CombatProfile(10f, 100f, 2f, 2f, 1);

        float longRangeOutcome = ExpectedOutcomeCalculator.EvaluateProfiles(
            new List<ExpectedOutcomeCalculator.CombatProfile> { longRange },
            new List<ExpectedOutcomeCalculator.CombatProfile> { opponent });
        float shortRangeOutcome = ExpectedOutcomeCalculator.EvaluateProfiles(
            new List<ExpectedOutcomeCalculator.CombatProfile> { shortRange },
            new List<ExpectedOutcomeCalculator.CombatProfile> { opponent });

        Assert.That(longRangeOutcome, Is.LessThan(shortRangeOutcome));
    }

    [Test]
    public void AoeTargetLimit_CapsTargetMultiplier()
    {
        var limitedAoe = new ExpectedOutcomeCalculator.CombatProfile(10f, 100f, 3f, 2f, 2);
        var unlimitedAoe = new ExpectedOutcomeCalculator.CombatProfile(10f, 100f, 3f, 2f, int.MaxValue);
        var twoTargets = new List<ExpectedOutcomeCalculator.CombatProfile>
        {
            new ExpectedOutcomeCalculator.CombatProfile(2f, 20f, 1f, 2f, 1),
            new ExpectedOutcomeCalculator.CombatProfile(2f, 20f, 1f, 2f, 1)
        };

        float limitedOutcome = ExpectedOutcomeCalculator.EvaluateProfiles(
            new List<ExpectedOutcomeCalculator.CombatProfile> { limitedAoe }, twoTargets);
        float unlimitedOutcome = ExpectedOutcomeCalculator.EvaluateProfiles(
            new List<ExpectedOutcomeCalculator.CombatProfile> { unlimitedAoe }, twoTargets);

        Assert.That(unlimitedOutcome, Is.EqualTo(limitedOutcome).Within(0.0001f));
    }
}
#endif
