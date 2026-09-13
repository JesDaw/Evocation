using UnityEngine;
using UnityEngine.Events;

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

    /// <summary>
    /// Positive = enemy favored overall, negative = allies/player favored.
    /// Each zone's outcome is weighted by how close that zone is to the base
    /// it threatens — an enemy-favored zone matters more near the player base,
    /// a player-favored zone matters more near the enemy base. Tune/remove the
    /// weighting below if you just want a flat sum instead.
    /// </summary>
    public float CalculateOverallExpectedOutcome()
    {
        float total = 0f;
        float baseDistance;
        if(PlayerBase != null && EnemyBase != null)
        {
             baseDistance = Vector3.Distance(PlayerBase.position, EnemyBase.position);
        }
        else
        {
                baseDistance= 1f;
        }

        foreach (var zone in zones)
        {
            if (zone == null) continue;

            float outcome = zone.ExpectedOutcome();
            float weight = 1f;

            if (PlayerBase != null && EnemyBase != null && baseDistance > 0f)
            {
                Vector3 relevantBase;
                 if (outcome > 0f)
                {
                    relevantBase = PlayerBase.position;
                }
                else
                {
                    relevantBase = EnemyBase.position;
                }
                float distanceToRelevantBase = Mathf.Abs(zone.CenterOfPower() - relevantBase.x);
                weight = Mathf.Clamp01(1f - (distanceToRelevantBase / baseDistance));
            }

            total += outcome * weight;
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