using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// this is how the AI will track the state of the game, it is exclusivly for the AIs decision making
/// </summary>
public class UnitTracker : MonoBehaviour
{
    public static UnitTracker Instance { get; private set; }

    [SerializeField] MapZone[] zones;
    [SerializeField] Transform PlayerBase;
    [SerializeField] Transform EnemyBase;
    [SerializeField] UnityEvent WinGame;
    [SerializeField] UnityEvent LooseGame;

    void Awake()
    {
        Instance = this;
    }

    public MapZone GetZone(int zoneNumber)
    {
        foreach (var zone in zones)
        {
            if (zone != null && zone.ZoneNumber == zoneNumber)
                return zone;
        }
        Debug.LogWarning($"UnitTracker: no zone found with ZoneNumber {zoneNumber}");
        return null;
    }

    public float CalculateWeightedAIThreatLevel()
    {
        return CalculateOverallExpectedOutcome(AIWeighted: true);
    }

    /// <summary>
    /// Positive = Player favored overall, negative = Ai favored.
    /// Each zone's outcome is weighted by how close that zone is to the base
    /// it threatens — an enemy-favored zone matters more near the player base,
    /// a player-favored zone matters more near the enemy base. Tune/remove the
    /// weighting below if you just want a flat sum instead.
    /// </summary>
    public float CalculateOverallExpectedOutcome(ScriptableStats extraCharacter = null, MapZone relaventZone = null, bool AIWeighted = false)
    {
        float total = 0f;
        float maxDistance = 0f;
        bool relaventZoneMatched = false;

        if (PlayerBase != null && EnemyBase != null)
        {
            maxDistance = Vector3.Distance(PlayerBase.position, EnemyBase.position);
        }

        foreach (var zone in zones)
        {
            if (zone == null) continue;

            bool isRelevantZone = (zone == relaventZone);
            if (isRelevantZone) relaventZoneMatched = true;

            float outcome = isRelevantZone ? zone.ExpectedOutcome(extraCharacter) : zone.ExpectedOutcome();
            float weight = 1f;

            if (AIWeighted && maxDistance > 0f)
            {
                float distanceToRelevantBase = Mathf.Abs(zone.CenterOfPower() - EnemyBase.position.x);
                float distanceRatio = Mathf.Clamp01(distanceToRelevantBase / maxDistance);
                weight = Mathf.Pow(1f - distanceRatio, 2);
            }

            total += outcome * weight * 100;
        }

        if (relaventZone != null && !relaventZoneMatched)
            Debug.LogWarning($"[UnitTracker] relaventZone '{relaventZone.name}' was passed in but doesn't match any zone in the tracked zones[] array — extraCharacter had no effect this call.");

        return total;
    }

    public void CompareUnitProximityToBases()
    {
        float outcome = CalculateOverallExpectedOutcome();
        if (outcome < 0f) WinGame?.Invoke();
        else LooseGame?.Invoke();
    }
}