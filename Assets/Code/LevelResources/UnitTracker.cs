using System.Collections.Generic;
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
        return CalculateOverallExpectedOutcome(AIWeighted: true);
    }

    /// <summary>
    /// Positive = Player/Allies favored, negative = AI/Enemy favored.
    /// Evaluates all live units across the map weighted by distance to EnemyBase so the AI
    /// sees incoming swarms, bosses, and allied buff targets even when aiSpawnZone is empty.
    /// </summary>
    public float CalculateOverallExpectedOutcome(
        ScriptableStats extraCharacter = null,
        MapZone relaventZone = null,
        bool AIWeighted = false,
        int extraCharacterCopies = 1)
    {
        if (zones == null || zones.Length == 0) return 0f;

        // Gather unique live units across all zones so units straddling zone borders aren't double-counted.
        List<Stats> allEnemyUnits = new List<Stats>();
        List<Stats> allPlayerUnits = new List<Stats>();
        HashSet<Stats> seen = new HashSet<Stats>();

        foreach (MapZone zone in zones)
        {
            if (zone == null) continue;
            CollectValidUnits(zone.EnemyUnits, allEnemyUnits, seen);
            CollectValidUnits(zone.PlayerUnits, allPlayerUnits, seen);
            CollectValidUnits(zone.AllyUnits, allPlayerUnits, seen);
        }

        if (AIWeighted && EnemyBase != null)
        {
            float maxDistance = (PlayerBase != null)
                ? Vector3.Distance(PlayerBase.position, EnemyBase.position)
                : 100f;

            return ExpectedOutcomeCalculator.CalculateDistanceWeightedOutcome(
                allEnemyUnits,
                allPlayerUnits,
                EnemyBase.position,
                maxDistance,
                extraCharacter,
                extraCharacterCopies) * 100f;
        }

        // Unweighted fallback across zones
        return ExpectedOutcomeCalculator.CalculateExpectedOutcome(
            allEnemyUnits,
            allPlayerUnits,
            extraCharacter,
            extraCharacterCopies) * 100f;
    }

    static void CollectValidUnits(List<GameObject> source, List<Stats> destination, HashSet<Stats> seen)
    {
        if (source == null) return;
        for (int i = source.Count - 1; i >= 0; i--)
        {
            GameObject obj = source[i];
            if (obj == null) continue;
            if (!obj.TryGetComponent(out Stats stats) || stats._IsDead || stats._CurrentHealth <= 0f)
                continue;
            if (seen.Add(stats))
                destination.Add(stats);
        }
    }

    public void CompareUnitProximityToBases()
    {
        float outcome = CalculateOverallExpectedOutcome();
        if (outcome < 0f) WinGame?.Invoke();
        else LooseGame?.Invoke();
    }
}