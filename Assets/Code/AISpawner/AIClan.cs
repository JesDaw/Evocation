using UnityEngine;

[CreateAssetMenu(fileName = "New Clan", menuName = "AI Clan")]
public class AIClan : ScriptableObject
{
    public AIClanPhase[] Phases;

    [Header("Lane Routing Personality")]
    [Tooltip("Use this clan's routing thresholds at every fork. When disabled, forks keep their existing scene-specific thresholds.")]
    public bool useClanLaneRoutingSettings = false;

    [Tooltip("Threshold when the sum of reachable lane outcomes favors the Player (positive or zero). Larger values make concentrating on AI-favored lanes more likely.")]
    [Min(0f)] public float netPositiveExpectedOutcomeDiscrepancyWindow = 0f;

    [Tooltip("Threshold when the sum of reachable lane outcomes favors the AI (negative). Larger values make concentrating on AI-favored lanes more likely.")]
    [Min(0f)] public float netNegativeExpectedOutcomeDiscrepancyWindow = 0f;
}

[System.Serializable]
public class AIClanPhase
{
    public string Name;
    public ScriptableStats[] Characters;
    public AnimationCurve AdvantageActivityCurve;
    public AnimationCurve DisadvantageActivityCurve;
}
