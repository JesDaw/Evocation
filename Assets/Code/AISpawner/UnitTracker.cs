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

    [Header("Experimental: Zone-Separated Outcomes")]
    [Tooltip("OFF preserves the previous whole-map calculation. ON evaluates each zone independently; units on different forks do not fight in the model.")]
    [SerializeField] bool useZoneSeparatedOutcomes = false;
    public bool ZoneSeparatedOutcomesEnabled => useZoneSeparatedOutcomes;

    public enum HypotheticalPlacementPolicy
    {
        // A newly purchased unit is evaluated as a possible reinforcement to ONE
        // enemy-occupied zone at a time. We average the benefits, rather than
        // cloning the unit into every zone simultaneously.
        AverageOpponentOccupiedZones,

        // Only the zone supplied by the caller (currently AILevelHarness.aiSpawnZone).
        // Useful as a diagnostic, but a spawn-zone-only assumption can overlook
        // future fights on forked routes.
        SuppliedZoneOnly
    }

    [SerializeField] HypotheticalPlacementPolicy hypotheticalPlacement =
        HypotheticalPlacementPolicy.AverageOpponentOccupiedZones;

    [Header("Experimental: Separate Urgency From Combat")]
    [Tooltip("Requires zone-separated outcomes. OFF preserves Step 2. ON applies a separate proximity-based urgency premium to player-favored zone scores AFTER calculating unweighted military advantage. Does not modify the combat model.")]
    [SerializeField] bool separateUrgencyFromCombat = false;

    [Tooltip("Maximum urgency multiplier for a player-favored zone when its closest live player/allied unit is at the AI base. One means no urgency effect. Distance is approximated along world X, not the navigation graph.")]
    [SerializeField, Min(1f)] float maxDefensiveUrgencyMultiplier = 2f;

    /// <summary>
    /// Signed unweighted military advantage in the same x100 scale as other tracker outcomes.
    /// This never uses proximity weights, even when the experimental urgency mode is enabled.
    /// A candidate is not included in this diagnostic.
    /// </summary>
    public float CalculateMilitaryAdvantage()
    {
        return CalculateOverallExpectedOutcome(AIWeighted: false);
    }

    /// <summary>
    /// Non-negative additional urgency pressure, in x100 outcome units, caused by proximity.
    /// Returns zero unless BOTH experimental settings are enabled.
    /// Does not alter combat stats; it is only the premium on player-favored local outcomes.
    /// </summary>
    public float CalculateDefensiveUrgencyPressure()
    {
        if (!useZoneSeparatedOutcomes || !separateUrgencyFromCombat || EnemyBase == null)
            return 0f;

        float sum = 0f;
        List<ZoneCombatSnapshot> snapshots = BuildZoneSnapshots();
        foreach (ZoneCombatSnapshot snapshot in snapshots)
        {
            float military = EvaluateZone(snapshot, false, null, 0);
            sum += Mathf.Max(0f, military) * (GetDefensiveUrgencyMultiplier(snapshot) - 1f);
        }
        return sum * 100f;
    }

    /// <summary>
    /// Experimental dimensionless relative-advantage signal in [-1, 1].
    /// Positive = player favored; negative = AI favored. Independent of the
    /// historical x100 threat scale and independent of candidate-purchase scoring.
    /// Uses the existing calculator's unweighted Lanchester-style outcome; no
    /// changes to its combat coefficients or score equations.
    /// </summary>
    public float CalculateRelativeMilitaryAdvantage()
    {
        if (!useZoneSeparatedOutcomes) return 0f;

        List<ZoneCombatSnapshot> snapshots = BuildZoneSnapshots();
        float signedSum = 0f;
        float totalWeight = 0f;

        foreach (ZoneCombatSnapshot snapshot in snapshots)
        {
            int aiCount = snapshot.ai.Count;
            int playerCount = snapshot.player.Count;
            int count = aiCount + playerCount;
            if (count == 0) continue;

            // Symmetric weight; prevents one occupied zone from counting as
            // much as a giant army without making large armies fully linear.
            float weight = Mathf.Sqrt(count);
            float relative = CalculateRelativeZoneAdvantage(snapshot, weight);

            signedSum += weight * relative;
            totalWeight += weight;
        }

        if (totalWeight <= 0f) return 0f;
        return Mathf.Clamp(signedSum / totalWeight, -1f, 1f);
    }

    /// <summary>
    /// Experimental activity input, signed and bounded to [-1, 1].
    /// The Step 3 urgency multiplier is applied only to player-favored local
    /// advantage, AFTER combat evaluation, and cannot exceed the bounded result.
    /// This method changes no existing outcome methods and no routing behavior.
    /// </summary>
    public float CalculateRelativeThreatForActivity()
    {
        if (!useZoneSeparatedOutcomes) return 0f;

        List<ZoneCombatSnapshot> snapshots = BuildZoneSnapshots();
        float signedSum = 0f;
        float totalWeight = 0f;
        foreach (ZoneCombatSnapshot snapshot in snapshots)
        {
            int aiCount = snapshot.ai.Count;
            int playerCount = snapshot.player.Count;
            int count = aiCount + playerCount;
            if (count == 0) continue;

            float weight = Mathf.Sqrt(count);
            float relative = CalculateRelativeZoneAdvantage(snapshot, weight);

            if (separateUrgencyFromCombat && relative > 0f)
                relative = Mathf.Min(1f, relative * GetDefensiveUrgencyMultiplier(snapshot));

            signedSum += weight * relative;
            totalWeight += weight;
        }

        return totalWeight > 0f ? Mathf.Clamp(signedSum / totalWeight, -1f, 1f) : 0f;
    }

    // Invert the existing score's size scaling. The underlying combat formula
    // remains owned by ExpectedOutcomeCalculator; this only normalizes its ratio.
    float CalculateRelativeZoneAdvantage(ZoneCombatSnapshot snapshot, float sqrtCount)
    {
        if (snapshot.ai.Count == 0) return 1f;
        if (snapshot.player.Count == 0) return -1f;

        float logRatio = EvaluateZone(snapshot, false, null, 0) / sqrtCount;
        float expNegAbs = Mathf.Exp(-Mathf.Abs(logRatio));
        float magnitude = (1f - expNegAbs) / (1f + expNegAbs);
        return Mathf.Sign(logRatio) * magnitude;
    }

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
    /// In experimental zone-separated mode, returns a SUM of zone-local outcomes.
    /// For candidate evaluation, computes the average gain from adding the candidate
    /// to ONE opponent-occupied zone, rather than adding it to all zones at once.
    /// The weighted mode retains Step 2 profile weighting unless the separate
    /// urgency option is enabled; then it weights local outcomes after combat evaluation.
    /// </summary>
    public float CalculateOverallExpectedOutcome(
        ScriptableStats extraCharacter = null,
        MapZone relaventZone = null,
        bool AIWeighted = false,
        int extraCharacterCopies = 1)
    {
        if (zones == null || zones.Length == 0) return 0f;

        if (useZoneSeparatedOutcomes)
            return CalculateZoneSeparatedOutcome(extraCharacter, relaventZone, AIWeighted, extraCharacterCopies);

        // Legacy whole-map behavior. Keep this branch unchanged for A/B comparison.
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

        return ExpectedOutcomeCalculator.CalculateExpectedOutcome(
            allEnemyUnits,
            allPlayerUnits,
            extraCharacter,
            extraCharacterCopies) * 100f;
    }

    sealed class ZoneCombatSnapshot
    {
        public MapZone zone;
        public readonly List<Stats> ai = new List<Stats>();
        public readonly List<Stats> player = new List<Stats>();
    }

    float CalculateZoneSeparatedOutcome(ScriptableStats candidate, MapZone suppliedZone,
        bool weighted, int copies)
    {
        // A unit whose collider straddles two zones is assigned to the first
        // registered zone. This prevents double counting within one snapshot.
        // The configured zones array order therefore resolves boundary overlap.
        List<ZoneCombatSnapshot> snapshots = BuildZoneSnapshots();
        if (snapshots.Count == 0) return 0f;

        float baseline = 0f;
        for (int i = 0; i < snapshots.Count; i++)
            baseline += EvaluateZone(snapshots[i], weighted, null, 0);

        baseline *= 100f; // Retain public result units and historical diagnostics scale.
        if (candidate == null || copies <= 0) return baseline;

        float accumulatedGain = 0f;
        int eligibleCount = 0;
        for (int i = 0; i < snapshots.Count; i++)
        {
            ZoneCombatSnapshot snapshot = snapshots[i];
            if (hypotheticalPlacement == HypotheticalPlacementPolicy.SuppliedZoneOnly)
            {
                if (snapshot.zone != suppliedZone) continue;
            }
            else
            {
                // Only evaluate the reinforcement where opponents currently exist.
                // This is a tactical heuristic, NOT a physical path-reachability test.
                if (snapshot.player.Count == 0) continue;
            }

            float before = EvaluateZone(snapshot, weighted, null, 0);
            float after = EvaluateZone(snapshot, weighted, candidate, copies);
            accumulatedGain += before - after;
            eligibleCount++;
        }

        // When no zone qualifies, no immediate tactical improvement is predicted.
        // Other purchase utilities are still used by the unchanged harness.
        if (eligibleCount == 0) return baseline;

        float expectedGain = accumulatedGain / eligibleCount;
        return baseline - expectedGain * 100f;
    }

    List<ZoneCombatSnapshot> BuildZoneSnapshots()
    {
        var result = new List<ZoneCombatSnapshot>();
        var seen = new HashSet<Stats>();
        var seenZones = new HashSet<MapZone>();

        foreach (MapZone zone in zones)
        {
            if (zone == null || !seenZones.Add(zone)) continue;
            var snapshot = new ZoneCombatSnapshot { zone = zone };
            CollectValidUnits(zone.EnemyUnits, snapshot.ai, seen);
            CollectValidUnits(zone.PlayerUnits, snapshot.player, seen);
            CollectValidUnits(zone.AllyUnits, snapshot.player, seen);
            result.Add(snapshot);
        }
        return result;
    }

    float EvaluateZone(ZoneCombatSnapshot snapshot, bool weighted,
        ScriptableStats candidate, int copies)
    {
        if (weighted && separateUrgencyFromCombat)
        {
            // Combat effectiveness is evaluated with NO distance modifiers.
            // Only the strategic importance of an unfavorable local outcome changes.
            float military = ExpectedOutcomeCalculator.CalculateExpectedOutcome(
                snapshot.ai, snapshot.player, candidate, copies);
            float multiplier = GetDefensiveUrgencyMultiplier(snapshot);
            return military + Mathf.Max(0f, military) * (multiplier - 1f);
        }

        if (weighted && EnemyBase != null)
        {
            float maxDistance = PlayerBase != null
                ? Vector3.Distance(PlayerBase.position, EnemyBase.position)
                : 100f;
            return ExpectedOutcomeCalculator.CalculateDistanceWeightedOutcome(
                snapshot.ai, snapshot.player, EnemyBase.position, maxDistance,
                candidate, copies);
        }

        return ExpectedOutcomeCalculator.CalculateExpectedOutcome(
            snapshot.ai, snapshot.player, candidate, copies);
    }

    /// <summary>
    /// Uses closest live player/allied unit in this zone as a simple local urgency proxy.
    /// The graph distance around forks may differ from this X-only estimate. The same
    /// multiplier applies before/after a hypothetical reinforcement, so only the
    /// local military score changes between the two candidate evaluations.
    /// </summary>
    float GetDefensiveUrgencyMultiplier(ZoneCombatSnapshot snapshot)
    {
        if (EnemyBase == null || snapshot.player.Count == 0) return 1f;

        float fullDistance = PlayerBase != null
            ? Mathf.Abs(PlayerBase.position.x - EnemyBase.position.x)
            : 100f;
        fullDistance = Mathf.Max(1f, fullDistance);
        float nearest = float.PositiveInfinity;

        foreach (Stats player in snapshot.player)
        {
            if (player == null) continue;
            float distance = Mathf.Abs(player.transform.position.x - EnemyBase.position.x);
            if (distance < nearest) nearest = distance;
        }

        if (float.IsInfinity(nearest)) return 1f;
        float proximity = 1f - Mathf.Clamp01(nearest / fullDistance);
        return Mathf.Lerp(1f, Mathf.Max(1f, maxDefensiveUrgencyMultiplier), proximity);
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
