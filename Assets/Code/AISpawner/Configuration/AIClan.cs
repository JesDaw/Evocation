
using UnityEngine;

[CreateAssetMenu(fileName = "New Clan", menuName = "AI Clan")]
public class AIClan : ScriptableObject
{
    public AIClanPhase[] Phases;

    [Header("AI Personality")]
    [Tooltip("0 = frequent small deployments; 1 = larger coordinated waves. Reserved for the tactical planner.")]
    [Range(0f, 1f)]
    public float waveConcentration = 0.5f;

    [Tooltip("0 = primarily evaluate military progress; 1 = strongly value map objectives. Reserved for operational planning.")]
    [Range(0f, 1f)]
    public float positionalValuation = 0.5f;

    [Tooltip("0 = respond to opponent actions; 1 = proactively pursue objectives. Does not directly control spending.")]
    [Range(0f, 1f)]
    public float initiative = 0.5f;

    [Tooltip("0 = prefer earlier spending; 1 = tolerate longer savings periods for worthwhile plans. Reserved for the economic planner.")]
    [Range(0f, 1f)]
    public float economicPatience = 0.5f;

    [Header("Lane Routing Personality")]
    [Tooltip("Use this clan's routing thresholds at every fork. When disabled, forks keep their existing scene-specific thresholds.")]
    public bool useClanLaneRoutingSettings = false;

    [Tooltip("Threshold when the sum of reachable lane outcomes favors the Player (positive or zero). Larger values make concentrating on AI-favored lanes more likely.")]
    [Min(0f)]
    public float netPositiveExpectedOutcomeDiscrepancyWindow = 0f;

    [Tooltip("Threshold when the sum of reachable lane outcomes favors the AI (negative). Larger values make concentrating on AI-favored lanes more likely.")]
    [Min(0f)]
    public float netNegativeExpectedOutcomeDiscrepancyWindow = 0f;

}

[System.Serializable]
public class AIClanPhase
{
    public string Name;
    public ScriptableStats[] Characters;
    public AnimationCurve AdvantageActivityCurve;
    public AnimationCurve DisadvantageActivityCurve;
}
