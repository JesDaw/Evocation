using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Battlefield-state provider used by the AI.
/// ExpectedOutcome sign convention: positive = Player/Allies favored; negative = AI/Enemy favored.
/// </summary>
public class UnitTracker : MonoBehaviour
{
    public static UnitTracker Instance { get; private set; }

    [SerializeField] public MapZone[] zones;
    [SerializeField] Transform PlayerBase;
    [SerializeField] Transform EnemyBase;
    [SerializeField] UnityEvent WinGame;
    [SerializeField] UnityEvent LooseGame;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("UnitTracker: more than one instance exists. The newest instance will replace the previous one.");
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public MapZone GetZone(int zoneNumber)
    {
        if (zones == null) return null;
        foreach (MapZone zone in zones)
        {
            if (zone != null && zone.ZoneNumber == zoneNumber) return zone;
        }
        Debug.LogWarning($"UnitTracker: no zone found with ZoneNumber {zoneNumber}");
        return null;
    }

    public float CalculateWeightedAIThreatLevel()
    {
        // Kept compatible with existing callers and spending curves.
        return CalculateOverallExpectedOutcome(AIWeighted: true);
    }

    /// <summary>
    /// Positive = Player/Allies favored, negative = AI/Enemy favored.
    /// When AIWeighted is true, zones closer to the AI base (EnemyBase) receive more weight,
    /// because a Player-favored battle there is a more immediate threat to the AI.
    /// </summary>
    public float CalculateOverallExpectedOutcome(ScriptableStats extraCharacter = null, MapZone relaventZone = null, bool AIWeighted = false)
    {
        if (zones == null || zones.Length == 0) return 0f;

        float total = 0f;
        float maxDistance = 0f;
        bool relevantZoneMatched = false;

        if (PlayerBase != null && EnemyBase != null)
            maxDistance = Vector3.Distance(PlayerBase.position, EnemyBase.position);

        foreach (MapZone zone in zones)
        {
            if (zone == null) continue;

            bool isRelevantZone = zone == relaventZone;
            if (isRelevantZone) relevantZoneMatched = true;

            float outcome = isRelevantZone ? zone.ExpectedOutcome(extraCharacter) : zone.ExpectedOutcome();
            float weight = 1f;

            if (AIWeighted && EnemyBase != null && maxDistance > 0f)
            {
                float distanceToAIBase = Mathf.Abs(zone.CenterOfPower() - EnemyBase.position.x);
                float distanceRatio = Mathf.Clamp01(distanceToAIBase / maxDistance);
                weight = Mathf.Pow(1f - distanceRatio, 2f);
            }

            total += outcome * weight * 100f;
        }

        if (relaventZone != null && !relevantZoneMatched)
        {
            Debug.LogWarning($"[UnitTracker] Zone '{relaventZone.name}' was passed in but is not in zones[]. The candidate unit was not included in this evaluation.");
        }

        return total;
    }

    public void CompareUnitProximityToBases()
    {
        float outcome = CalculateOverallExpectedOutcome();
        if (outcome < 0f) WinGame?.Invoke();
        else LooseGame?.Invoke();
    }
}
