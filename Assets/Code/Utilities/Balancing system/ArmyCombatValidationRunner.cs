using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controlled, real-CPU army matchups for auditing ExpectedOutcomeCalculator.
/// Place in an isolated combat scene with normal economy / AI purchases / unrelated spawning disabled.
/// Does not modify production balance assets or BalanceSimulationRunner.
/// </summary>
public class ArmyCombatValidationRunner : MonoBehaviour
{
    public enum Formation { Grouped, Spread, Staggered }
    public enum TrialOutcome { TeamA, TeamB, Draw, Timeout, Invalid }
    public enum SuiteMode { Manual, Generated }
    public enum GeneratedCategory { EqualCost, UnequalCost, NumericalAdvantage, MixedArmies, Mirror }


    [Serializable]
    public class ArmyEntry
    {
        [Tooltip("ScriptableStats asset to spawn. Uses the unit's actual actions, health, movement and cost; no synthetic stats are created.")]
        public ScriptableStats unit;
        [Tooltip("How many copies of this exact unit to spawn on this team. Multiple entries can make a mixed army.")]
        [Min(1)] public int count = 1;
    }

    [Serializable]
    public class Scenario
    {
        [Tooltip("Unique label saved in every CSV row. Generated scenarios use stable IDs derived from seed and generation order.")]
        public string scenarioId = "matchup_01";
        [Tooltip("Team A composition. Normally starts on the allied/left side; reverse orientation swaps its side.")]
        public List<ArmyEntry> teamA = new List<ArmyEntry>();
        [Tooltip("Team B composition. Normally starts on the enemy/right side; reverse orientation swaps its side.")]
        public List<ArmyEntry> teamB = new List<ArmyEntry>();
        [Tooltip("How Team A's units are placed: Grouped = compact cluster; Spread = distribute along Y; Staggered = offset rows.")]
        public Formation formationA = Formation.Grouped;
        [Tooltip("How Team B's units are placed. Formation changes initial positioning, not the unit combat stats.")]
        public Formation formationB = Formation.Grouped;
        [Tooltip("World-space X distance BETWEEN the centers of the two opposing armies at spawn. Example: 16 means each army center is 8 units from Center X. Not spacing between units.")]
        [Min(0.1f)] public float separation = 16f;
        [Tooltip("World-space X spacing WITHIN each army. Larger values stretch units front-to-back. This is not the distance between the two opposing armies; use Separation for that.")]
        [Min(0.05f)] public float unitSpacing = 1.25f;
        [Tooltip("World-space Y spacing WITHIN each army. Larger values spread units vertically; this can change how many targets an AOE attack can hit.")]
        [Min(0f)] public float rowSpacing = 0.65f;
        [Tooltip("Maximum units per staggered row before starting another. 0 = all units in one row. Only used by Staggered formation.")]
        [Min(0)] public int maxPerRow = 5;
        [Tooltip("Number of battles to repeat for EACH orientation. Example: 3 repetitions with Reverse Orientation enabled runs 6 battles.")]
        [Min(1)] public int repetitions = 10;
        [Tooltip("Run each matchup in both orientations by swapping which spawner/faction and physical side each team uses. Helps identify consistent side/order advantages.")]
        public bool reverseOrientation = true;
        [Tooltip("Maximum simulated combat seconds per trial, not wall-clock seconds. Unresolved fights are recorded as Timeout rather than a win.")]
        [Min(1f)] public float durationSeconds = 60f;
        [HideInInspector] public string generatedCategory = "Manual";
    }

    [Header("Real CPU Spawners (configured Allies and Enemy)")]
    public SpawnObjects alliedSpawner;
    public SpawnObjects enemySpawner;

    [Header("Scenario Selection")]
    [Tooltip("Manual runs your editable scenarios list. Generated runs a reproducible batch assembled from Unit Pool. Manual scenarios are never overwritten.")]
    public SuiteMode suiteMode = SuiteMode.Manual;
    [Tooltip("Editable, hand-authored scenarios. Use these to investigate a generated matchup by promoting it into this list.")]
    public List<Scenario> scenarios = new List<Scenario>();

    [Header("Generated Suite: Roster and Limits")]
    [Tooltip("Only these existing ScriptableStats assets may be used by the generator. Drag the complete roster here; null and duplicate entries are ignored.")]
    public List<ScriptableStats> unitPool = new List<ScriptableStats>();
    [Tooltip("Unique matchup definitions to generate. Each matchup is repeated and orientation-tested according to the settings below.")]
    [Min(1)] public int generatedMatchupCount = 100;
    [Tooltip("Seed for the entire suite. Same seed, roster order and generator settings produce the same matchup definitions.")]
    public int randomSeed = 1729;
    [Tooltip("Lower bound for total living unit count on either side in generated matches.")]
    [Min(1)] public int minUnitsPerTeam = 1;
    [Tooltip("Upper bound for total unit count on either side. Start small to avoid very large or slow fights.")]
    [Min(1)] public int maxUnitsPerTeam = 8;
    [Tooltip("Maximum distinct ScriptableStats unit TYPES in each generated army. Allows mixed-unit interactions.")]
    [Min(1)] public int maxTypesPerTeam = 3;
    [Tooltip("For EqualCost matchups, maximum acceptable relative difference in the two armies' total spawn costs. 0.15 allows a 15% cost mismatch relative to the higher cost.")]
    [Range(0f, 1f)] public float equalCostTolerance = 0.15f;
    [Tooltip("Repeats per orientation for generated matchups only. Low repeats are useful for a first large survey.")]
    [Min(1)] public int generatedRepetitions = 3;
    [Tooltip("When true, generated matchups are fought in both orientations, doubling the number of trials.")]
    public bool generatedReverseOrientation = true;
    [Tooltip("Maximum simulated seconds per generated battle.")]
    [Min(1f)] public float generatedDurationSeconds = 40f;
    [Tooltip("Initial X distance between the centers of generated opposing armies; NOT the spacing of units within each army.")]
    [Min(0.1f)] public float generatedSeparation = 16f;
    [Tooltip("Initial X spacing between units within each generated army (front-to-back).")]
    [Min(0.05f)] public float generatedUnitSpacing = 1.25f;
    [Tooltip("Initial Y spacing between units within each generated army (vertical clustering).")]
    [Min(0f)] public float generatedRowSpacing = 0.65f;
    [Tooltip("Maximum units per row when generated formation is Staggered.")]
    [Min(0)] public int generatedMaxPerRow = 5;
    [Tooltip("Initial formation for generated Team A. Use Grouped initially; compare Spread in a separate controlled suite.")]
    public Formation generatedFormationA = Formation.Grouped;
    [Tooltip("Initial formation for generated Team B.")]
    public Formation generatedFormationB = Formation.Grouped;

    [Header("Generated Suite: Inspect / Replay")]
    [Tooltip("Read-only preview of the last generated batch. Press Preview / Regenerate to fill it. Editing a preview does not affect saved manual scenarios.")]
    public List<Scenario> generatedPreview = new List<Scenario>();
    [Tooltip("Index (0 based) into Generated Preview to copy into the manual list. Set this after finding the matchup ID in a CSV.")]
    [Min(0)] public int replayPreviewIndex;
    [Tooltip("Optional exact scenario_id from the exported CSV. If set, it takes precedence over Replay Preview Index when copying to Manual Scenarios.")]
    public string replayMatchupId = "";


    [Header("Trial Environment")]
    [Tooltip("Use this world-space X as the combat center. Team A is left on normal trials.")]
    public float centerX = 0f;
    [Tooltip("Optional world-space Y override; both teams use their spawner Y when disabled.")]
    public bool overrideY;
    public float centerY = 0f;
    [Range(1f, 15f)] public float simulationTimeScale = 1f;
    [Min(0f)] public float startupGraceSeconds = 0.15f;
    [Tooltip("Abort if untracked living units already exist; avoids polluted trials.")]
    public bool requireEmptyBattlefield = true;
    [Tooltip("Components such as AILevelHarness that the test should temporarily disable. Do not include spawners, Stats, or combat components.")]
    public List<MonoBehaviour> suspendDuringSuite = new List<MonoBehaviour>();

    [Header("Output")]
    public string outputPrefix = "ArmyCombatValidation";
    public bool exportWhenFinished = true;
    public bool verboseLogs = true;

    [Header("Read-only Run Status")]
    public bool isRunning;
    public string status = "Idle";
    public int completedTrials;
    public int totalTrials;
    public List<TrialResult> trialResults = new List<TrialResult>();
    public List<ScenarioSummary> summaries = new List<ScenarioSummary>();

    [Serializable]
    public class TrialResult
    {
        public string scenarioId;
        public string category;
        public int scenarioIndex, repeat, orientation;
        public string teamADefinition, teamBDefinition;
        public string formationA, formationB;
        public float separation, spacing, durationLimit;
        public float predictedOutcome;
        public string predictedSide;
        public string actualSide;
        public float duration;
        public int aInitial, bInitial, aSurvivors, bSurvivors;
        public float aInitialHp, bInitialHp, aRemainingHp, bRemainingHp;
        public float aDamageReceived, bDamageReceived;
        public float aAdvanceMeters, bAdvanceMeters;
        public float firstHitSeconds;
        public string note;
    }

    [Serializable]
    public class ScenarioSummary
    {
        public string scenarioId;
        public string category;
        public int scenarioIndex, completed, invalid, aWins, bWins, draws, timeouts;
        public float prediction, averageDuration, averageAHealthFraction, averageBHealthFraction;
        public int predictedSideCorrect, decisiveTrials;
    }

    private class ObservedUnit
    {
        public GameObject go;
        public Stats stats;
        public Vector3 startingPosition;
        public float previousHp;
        public float initialHp;
        public float totalDamageReceived;
        public bool teamA;
        public int forwardSign;
    }

    readonly List<ObservedUnit> _units = new List<ObservedUnit>();
    readonly List<GameObject> _spawned = new List<GameObject>();
    readonly List<MonoBehaviour> _suspended = new List<MonoBehaviour>();
    readonly List<bool> _previousEnabled = new List<bool>();
    Coroutine _runningCoroutine;
    float _previousTimeScale;
    bool _ownsTimeScale;
    string _suiteId;
    readonly List<Scenario> _suiteScenarios = new List<Scenario>();
    SuiteMode _activeMode;
    int _activeSeed;

    [ContextMenu("Preview / Regenerate Random Matchups")]
    public void RegeneratePreview()
    {
        if (isRunning) { Debug.LogWarning("[ArmyValidation] Cannot regenerate while running.", this); return; }
        string error = ValidateGeneratorSettings();
        if (error != null) { Debug.LogError("[ArmyValidation] " + error, this); return; }
        generatedPreview = GenerateMatchups();
        status = "Generated " + generatedPreview.Count + " reproducible matchups (seed " + randomSeed + ").";
        Debug.Log("[ArmyValidation] " + status, this);
    }

    [ContextMenu("Copy Selected Preview Matchup To Manual Scenarios")]
    public void PromotePreviewScenario()
    {
        if (isRunning) { Debug.LogWarning("[ArmyValidation] Stop the suite before copying a scenario.", this); return; }
        if (Application.isPlaying) { Debug.LogWarning("[ArmyValidation] Copy the scenario OUTSIDE Play mode so Unity retains the manual scenario.", this); return; }
        int index = replayPreviewIndex;
        if (!string.IsNullOrWhiteSpace(replayMatchupId) && generatedPreview != null)
        {
            index = -1;
            for (int i = 0; i < generatedPreview.Count; i++)
                if (generatedPreview[i].scenarioId == replayMatchupId.Trim()) { index = i; break; }
        }
        if (generatedPreview == null || index < 0 || index >= generatedPreview.Count)
        { Debug.LogWarning("[ArmyValidation] Generate a preview with the original seed/settings, then supply a valid Replay Matchup Id or Replay Preview Index.", this); return; }
        Scenario copy = CloneScenario(generatedPreview[index]);
        // Keep the generated ID as a reproducibility key; ensure a unique name in the manual list.
        string baseId = copy.scenarioId + "_replay";
        string id = baseId;
        for (int suffix = 2; ContainsManualScenarioId(id); suffix++) id = baseId + "_" + suffix++;
        copy.scenarioId = id;
        scenarios.Add(copy);
        suiteMode = SuiteMode.Manual;
        status = "Copied " + id + " to Manual Scenarios. Switch to Play mode to inspect the battle.";
        Debug.Log("[ArmyValidation] " + status, this);
    }

    bool ContainsManualScenarioId(string id)
    {
        foreach (Scenario s in scenarios) if (s != null && s.scenarioId == id) return true;
        return false;
    }

    static Scenario CloneScenario(Scenario s)
    {
        Scenario copy = new Scenario
        {
            scenarioId = s.scenarioId, formationA = s.formationA, formationB = s.formationB,
            separation = s.separation, unitSpacing = s.unitSpacing, rowSpacing = s.rowSpacing,
            maxPerRow = s.maxPerRow, repetitions = s.repetitions,
            reverseOrientation = s.reverseOrientation, durationSeconds = s.durationSeconds,
            generatedCategory = s.generatedCategory
        };
        foreach (ArmyEntry e in s.teamA) copy.teamA.Add(new ArmyEntry { unit = e.unit, count = e.count });
        foreach (ArmyEntry e in s.teamB) copy.teamB.Add(new ArmyEntry { unit = e.unit, count = e.count });
        return copy;
    }

    string ValidateGeneratorSettings()
    {
        if (unitPool == null) return "Unit Pool must contain at least one unit.";
        int valid = 0;
        HashSet<ScriptableStats> unique = new HashSet<ScriptableStats>();
        foreach (ScriptableStats s in unitPool) if (s != null && unique.Add(s)) valid++;
        if (valid == 0) return "Unit Pool is empty: assign ScriptableStats assets.";
        if (generatedMatchupCount <= 0 || minUnitsPerTeam <= 0 || maxUnitsPerTeam < minUnitsPerTeam || maxTypesPerTeam <= 0)
            return "Invalid generated army size or matchup count.";
        if (generatedDurationSeconds <= 0f || generatedSeparation <= 0f || generatedUnitSpacing <= 0f || generatedRepetitions <= 0)
            return "Invalid generated duration, spacing, separation or repetitions.";
        if (valid == 1) Debug.LogWarning("[ArmyValidation] Only one unit in pool: many matchups will be mirrors.", this);
        return null;
    }

    List<Scenario> GenerateMatchups()
    {
        var pool = new List<ScriptableStats>();
        foreach (ScriptableStats u in unitPool) if (u != null && !pool.Contains(u)) pool.Add(u);
        var random = new System.Random(randomSeed);
        var generated = new List<Scenario>();
        var seen = new HashSet<string>();
        GeneratedCategory[] categories = (GeneratedCategory[])Enum.GetValues(typeof(GeneratedCategory));
        for (int i = 0; i < generatedMatchupCount; i++)
        {
            GeneratedCategory category = categories[i % categories.Length];
            Scenario candidate = null;
            string key = null;
            bool found = false;
            for (int attempt = 0; attempt < 240; attempt++)
            {
                candidate = BuildGeneratedCandidate(pool, random, category);
                key = Definition(candidate.teamA) + " || " + Definition(candidate.teamB);
                bool categoryOk = CategorySatisfied(candidate, category);
                if (categoryOk && !seen.Contains(key)) { found = true; break; }
            }
            if (!found)
            {
                Debug.LogWarning("[ArmyValidation] Could not generate unique " + category + " matchup #" + (i + 1) + " within 240 attempts. Trying any unique composition instead.", this);
                for (int attempt = 0; attempt < 240; attempt++)
                {
                    candidate = BuildGeneratedCandidate(pool, random, GeneratedCategory.UnequalCost);
                    key = Definition(candidate.teamA) + " || " + Definition(candidate.teamB);
                    if (Count(candidate.teamA) > 0 && Count(candidate.teamB) > 0 && !seen.Contains(key))
                    { found = true; category = GeneratedCategory.UnequalCost; break; }
                }
            }
            if (!found)
            {
                Debug.LogWarning("[ArmyValidation] Exhausted unique army combinations at " + generated.Count + " scenarios. Increase roster or army sizes.", this);
                return generated;
            }
            seen.Add(key);
            candidate.scenarioId = "gen_s" + randomSeed.ToString(CultureInfo.InvariantCulture) + "_" + (i + 1).ToString("D4", CultureInfo.InvariantCulture) + "_" + category;
            candidate.generatedCategory = category.ToString();
            generated.Add(candidate);
        }
        return generated;
    }

    Scenario BuildGeneratedCandidate(List<ScriptableStats> pool, System.Random random, GeneratedCategory category)
    {
        Scenario s = new Scenario
        {
            formationA = generatedFormationA, formationB = generatedFormationB,
            separation = generatedSeparation, unitSpacing = generatedUnitSpacing,
            rowSpacing = generatedRowSpacing, maxPerRow = generatedMaxPerRow,
            repetitions = generatedRepetitions, reverseOrientation = generatedReverseOrientation,
            durationSeconds = generatedDurationSeconds
        };
        int aCount = random.Next(minUnitsPerTeam, maxUnitsPerTeam + 1);
        int bCount = random.Next(minUnitsPerTeam, maxUnitsPerTeam + 1);
        if (category == GeneratedCategory.Mirror) bCount = aCount;
        if (category == GeneratedCategory.NumericalAdvantage)
        {
            if (maxUnitsPerTeam <= minUnitsPerTeam) return s;
            aCount = random.Next(minUnitsPerTeam, maxUnitsPerTeam);
            bCount = random.Next(aCount + 1, maxUnitsPerTeam + 1);
            if (random.Next(2) == 0) { int temp = aCount; aCount = bCount; bCount = temp; }
        }
        if (category == GeneratedCategory.MixedArmies && (pool.Count < 2 || maxTypesPerTeam < 2 || maxUnitsPerTeam < 2)) return s;
        if (category == GeneratedCategory.MixedArmies) { aCount = Mathf.Max(2, aCount); bCount = Mathf.Max(2, bCount); }
        int maxTypes = category == GeneratedCategory.MixedArmies ? Mathf.Min(maxTypesPerTeam, pool.Count) :
            category == GeneratedCategory.NumericalAdvantage ? 1 : Mathf.Min(maxTypesPerTeam, pool.Count);
        s.teamA = RandomArmy(pool, random, aCount, maxTypes, category == GeneratedCategory.MixedArmies);
        s.teamB = category == GeneratedCategory.Mirror ? CloneEntries(s.teamA) : RandomArmy(pool, random, bCount, maxTypes, category == GeneratedCategory.MixedArmies);
        return s;
    }

    static List<ArmyEntry> CloneEntries(List<ArmyEntry> source)
    {
        var entries = new List<ArmyEntry>();
        foreach (ArmyEntry e in source) entries.Add(new ArmyEntry { unit = e.unit, count = e.count });
        return entries;
    }

    static List<ArmyEntry> RandomArmy(List<ScriptableStats> pool, System.Random rng, int total, int maxTypes, bool requireMixed)
    {
        var entries = new List<ArmyEntry>();
        int kinds = Mathf.Min(total, Mathf.Min(maxTypes, pool.Count));
        int chosenTypes = requireMixed && kinds >= 2 ? rng.Next(2, kinds + 1) : rng.Next(1, kinds + 1);
        var available = new List<ScriptableStats>(pool);
        for (int i = 0; i < chosenTypes; i++)
        {
            int index = rng.Next(available.Count);
            entries.Add(new ArmyEntry { unit = available[index], count = 1 });
            available.RemoveAt(index);
        }
        for (int i = chosenTypes; i < total; i++) entries[rng.Next(entries.Count)].count++;
        entries.Sort((a, b) => string.CompareOrdinal(a.unit.name, b.unit.name));
        return entries;
    }

    bool CategorySatisfied(Scenario s, GeneratedCategory category)
    {
        if (Count(s.teamA) == 0 || Count(s.teamB) == 0) return false;
        int costA = ArmyCost(s.teamA), costB = ArmyCost(s.teamB);
        float gap = Mathf.Abs(costA - costB) / Mathf.Max(1f, Mathf.Max(costA, costB));
        if (category == GeneratedCategory.EqualCost) return gap <= equalCostTolerance;
        if (category == GeneratedCategory.UnequalCost) return gap >= Mathf.Max(0.3f, equalCostTolerance + 0.05f);
        if (category == GeneratedCategory.NumericalAdvantage) return Count(s.teamA) != Count(s.teamB);
        if (category == GeneratedCategory.MixedArmies) return s.teamA.Count >= 2 && s.teamB.Count >= 2;
        return Definition(s.teamA) == Definition(s.teamB);
    }

    static int ArmyCost(List<ArmyEntry> entries)
    {
        int total = 0;
        foreach (ArmyEntry e in entries) if (e != null && e.unit != null) total += Mathf.Max(0, e.unit._spawnCost) * e.count;
        return total;
    }


    public void RunSuite()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[ArmyValidation] Enter Play mode first.", this); return; }
        if (isRunning) return;
        _runningCoroutine = StartCoroutine(RunAll());
    }

    public void StopSuite()
    {
        if (!isRunning) return;
        if (_runningCoroutine != null) StopCoroutine(_runningCoroutine);
        _runningCoroutine = null;
        FinishSuite("Stopped by user", false);
    }

    void OnDisable()
    {
        if (isRunning) StopSuite();
    }

    void OnDestroy()
    {
        if (isRunning) StopSuite();
    }

    IEnumerator RunAll()
    {
        isRunning = true;
        completedTrials = 0;
        totalTrials = 0;
        trialResults.Clear();
        summaries.Clear();
        _suiteId = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        string error = ValidateConfiguration();
        if (error != null)
        {
            Debug.LogError("[ArmyValidation] " + error, this);
            FinishSuite("Invalid configuration: " + error, false);
            yield break;
        }

        _activeMode = suiteMode;
        _activeSeed = randomSeed;
        _suiteScenarios.Clear();
        if (_activeMode == SuiteMode.Generated)
        {
            // Always regenerate at run time from the seed: a stale preview cannot silently alter the experiment.
            generatedPreview = GenerateMatchups();
            foreach (Scenario s in generatedPreview) _suiteScenarios.Add(CloneScenario(s));
        }
        else foreach (Scenario s in scenarios) _suiteScenarios.Add(CloneScenario(s));
        if (_suiteScenarios.Count == 0)
        {
            FinishSuite("No valid scenarios generated; check roster, army limits, and category constraints.", false);
            yield break;
        }
        foreach (Scenario s in _suiteScenarios)
            totalTrials += Mathf.Max(1, s.repetitions) * (s.reverseOrientation ? 2 : 1);

        _previousTimeScale = Time.timeScale;
        _ownsTimeScale = true;
        SuspendBehaviours();
        Time.timeScale = simulationTimeScale;

        // Let pending destruction from any previous external test settle before first trial.
        yield return new WaitForFixedUpdate();
        for (int si = 0; si < _suiteScenarios.Count; si++)
        {
            Scenario s = _suiteScenarios[si];
            float prediction = Predict(s);
            for (int rep = 0; rep < Mathf.Max(1, s.repetitions); rep++)
            {
                int orientations = s.reverseOrientation ? 2 : 1;
                for (int orientation = 0; orientation < orientations; orientation++)
                {
                    status = s.scenarioId + " repeat " + (rep + 1) + "/" + s.repetitions + " orientation " + orientation;
                    TrialResult result = null;
                    yield return StartCoroutine(RunTrial(s, si, rep, orientation, prediction, r => result = r));
                    if (result != null) trialResults.Add(result);
                    completedTrials++;
                    if (verboseLogs && result != null)
                        Debug.Log("[ArmyValidation] " + s.scenarioId + " #" + (rep + 1) + " orientation " + orientation + ": predicted " + result.predictedOutcome.ToString("F3") + ", actual " + result.actualSide + ", " + result.duration.ToString("F2") + "s", this);
                    // Unity Destroy is deferred; do not overlap consecutive battles.
                    yield return new WaitForFixedUpdate();
                }
            }
            summaries.Add(Summarize(s, si));
        }
        FinishSuite("Finished " + completedTrials + "/" + totalTrials + " trials", exportWhenFinished);
    }

    string ValidateConfiguration()
    {
        if (alliedSpawner == null || enemySpawner == null || alliedSpawner == enemySpawner)
            return "Assign two distinct Allied and Enemy SpawnObjects components.";
        if (!alliedSpawner.spawningEnabled || !enemySpawner.spawningEnabled)
            return "Both spawners must have spawning enabled. Disable normal AI purchasing instead.";
        if (requireEmptyBattlefield && HasUntrackedLivingUnits()) return "Pre-existing live Stats units found. Use an isolated test scene or clear the battlefield.";
        if (suiteMode == SuiteMode.Generated) return ValidateGeneratorSettings();
        if (scenarios == null || scenarios.Count == 0) return "Add at least one manual scenario.";
        for (int i = 0; i < scenarios.Count; i++)
        {
            Scenario s = scenarios[i];
            if (s == null) return "Scenario " + i + " is null.";
            if (string.IsNullOrEmpty(s.scenarioId)) return "Scenario " + i + " has no ID.";
            if (Count(s.teamA) == 0 || Count(s.teamB) == 0) return "Scenario " + i + " needs both armies.";
            if (s.separation <= 0f || s.durationSeconds <= 0f || s.unitSpacing <= 0f) return "Scenario " + i + " has invalid distances or duration.";
        }
        if (requireEmptyBattlefield && HasUntrackedLivingUnits()) return "Pre-existing live Stats units found. Use an isolated test scene or clear the battlefield.";
        return null;
    }

    static int Count(List<ArmyEntry> entries)
    {
        int count = 0;
        if (entries == null) return 0;
        foreach (ArmyEntry e in entries)
            if (e != null && e.unit != null) count += Mathf.Max(0, e.count);
        return count;
    }

    static string Definition(List<ArmyEntry> entries)
    {
        StringBuilder sb = new StringBuilder();
        if (entries != null)
            foreach (ArmyEntry e in entries)
            {
                if (e == null || e.unit == null || e.count <= 0) continue;
                if (sb.Length > 0) sb.Append(" + ");
                sb.Append(e.count).Append('x').Append(e.unit.name);
            }
        return sb.ToString();
    }

    static float Predict(Scenario s)
    {
        // Production calculator API: AI/Enemy profiles FIRST, Player/Allies profiles SECOND.
        // Team A always represents Player/Allies for the signed prediction, even on orientation swap.
        List<ExpectedOutcomeCalculator.CombatProfile> ai = Profiles(s.teamB);
        List<ExpectedOutcomeCalculator.CombatProfile> player = Profiles(s.teamA);
        return ExpectedOutcomeCalculator.EvaluateProfiles(ai, player);
    }

    static List<ExpectedOutcomeCalculator.CombatProfile> Profiles(List<ArmyEntry> entries)
    {
        var result = new List<ExpectedOutcomeCalculator.CombatProfile>();
        if (entries == null) return result;
        foreach (ArmyEntry e in entries)
        {
            if (e == null || e.unit == null) continue;
            var profile = ExpectedOutcomeCalculator.BuildCandidateProfile(e.unit);
            for (int j = 0; j < Mathf.Max(0, e.count); j++) result.Add(profile);
        }
        return result;
    }

    IEnumerator RunTrial(Scenario s, int scenarioIndex, int rep, int orientation, float predicted, Action<TrialResult> completed)
    {
        CleanupSpawned();
        yield return new WaitForFixedUpdate();
        TrialResult r = new TrialResult
        {
            scenarioId = s.scenarioId, category = s.generatedCategory, scenarioIndex = scenarioIndex, repeat = rep + 1, orientation = orientation,
            teamADefinition = Definition(s.teamA), teamBDefinition = Definition(s.teamB),
            formationA = s.formationA.ToString(), formationB = s.formationB.ToString(),
            separation = s.separation, spacing = s.unitSpacing, durationLimit = s.durationSeconds,
            predictedOutcome = predicted, predictedSide = predicted > 0.0001f ? "TeamA" : predicted < -0.0001f ? "TeamB" : "Draw",
            actualSide = "Invalid", firstHitSeconds = -1f
        };

        // Reverse the physical teams, not just their coordinates: actual Enemy/Allies tag,
        // facing, and attack targets must continue to match the assigned spawners.
        bool aOnLeft = orientation == 0;
        SpawnObjects aSpawner = aOnLeft ? alliedSpawner : enemySpawner;
        SpawnObjects bSpawner = aOnLeft ? enemySpawner : alliedSpawner;
        float aCenter = centerX + (aOnLeft ? -0.5f : 0.5f) * s.separation;
        float bCenter = centerX + (aOnLeft ? 0.5f : -0.5f) * s.separation;
        bool aSuccess = SpawnArmy(s.teamA, aSpawner, true, aCenter, aOnLeft ? 1 : -1, s.formationA, s);
        bool bSuccess = aSuccess && SpawnArmy(s.teamB, bSpawner, false, bCenter, aOnLeft ? -1 : 1, s.formationB, s);
        if (!aSuccess || !bSuccess)
        {
            r.note = "Spawn failed (check console, prefab, spawner state, tags and layers).";
            CleanupSpawned(); completed(r); yield break;
        }
        Physics2D.SyncTransforms();
        r.aInitial = CountObserved(true); r.bInitial = CountObserved(false);
        r.aInitialHp = InitialHp(true); r.bInitialHp = InitialHp(false);
        if (verboseLogs && rep == 0 && orientation == 0)
            Debug.Log("[ArmyValidation] " + s.scenarioId + ": unweighted prediction " + predicted.ToString("F3") + " (positive=Team A); starting " + r.aInitial + " vs " + r.bInitial, this);

        // Startup() waits 0.1 game seconds. Do not count startup time as combat time.
        yield return new WaitForSeconds(Mathf.Max(0.11f, startupGraceSeconds));
        float elapsed = 0f;
        bool ended = false;
        while (elapsed < s.durationSeconds)
        {
            yield return new WaitForFixedUpdate();
            float dt = Time.fixedDeltaTime;
            elapsed += dt;
            SampleDamage(r, elapsed);
            int a = CountAlive(true), b = CountAlive(false);
            if (a == 0 || b == 0)
            {
                r.actualSide = a == 0 && b == 0 ? "Draw" : a > 0 ? "TeamA" : "TeamB";
                ended = true;
                break;
            }
        }
        if (!ended) r.actualSide = "Timeout";
        r.duration = elapsed;
        r.aSurvivors = CountAlive(true); r.bSurvivors = CountAlive(false);
        r.aRemainingHp = RemainingHp(true); r.bRemainingHp = RemainingHp(false);
        r.aAdvanceMeters = AverageAdvance(true); r.bAdvanceMeters = AverageAdvance(false);
        r.aDamageReceived = TotalDamageReceived(true); r.bDamageReceived = TotalDamageReceived(false);
        if (!ended) r.note = "Unresolved at duration limit; not scored as win or draw.";
        CleanupSpawned();
        completed(r);
    }

    bool SpawnArmy(List<ArmyEntry> entries, SpawnObjects spawner, bool isA, float armyCenterX, int forwardSign, Formation formation, Scenario s)
    {
        List<ScriptableStats> units = new List<ScriptableStats>();
        foreach (ArmyEntry e in entries)
            if (e != null && e.unit != null) for (int i = 0; i < Mathf.Max(0, e.count); i++) units.Add(e.unit);

        int n = units.Count;
        int perRow = s.maxPerRow <= 0 ? n : Mathf.Max(1, s.maxPerRow);
        int rows = Mathf.CeilToInt((float)n / perRow);
        float yBase = overrideY ? centerY : spawner.transform.position.y;
        float zBase = spawner.transform.position.z;
        for (int i = 0; i < n; i++)
        {
            int row = formation == Formation.Staggered ? i / perRow : 0;
            int index = formation == Formation.Staggered ? i % perRow : i;
            int inRow = formation == Formation.Staggered ? Mathf.Min(perRow, n - row * perRow) : n;
            // Grouped uses a compact two-column grid rather than overlapping colliders.
            int compactColumn = i % 2;
            int compactRow = i / 2;
            float xOffset = formation == Formation.Grouped ? (compactColumn - 0.5f) * s.unitSpacing * 0.55f :
                (index - (inRow - 1) * 0.5f) * s.unitSpacing;
            float yOffset = formation == Formation.Grouped ? (compactRow - (Mathf.CeilToInt(n / 2f) - 1) * 0.5f) * Mathf.Max(0.25f, s.rowSpacing * 0.6f) :
                (formation == Formation.Spread ? (index - (n - 1) * 0.5f) * s.rowSpacing :
                (row - (rows - 1) * 0.5f) * s.rowSpacing);
            if (formation == Formation.Staggered) xOffset += row * s.unitSpacing * 0.5f;
            // X spacing is a front/back file, not lateral spread. Y controls AOE clustering.
            Vector3 pos = new Vector3(armyCenterX - forwardSign * xOffset, yBase + yOffset, zBase);
            GameObject go = spawner.SpawnCPU(units[i]);
            if (go == null) return false;
            _spawned.Add(go);
            go.transform.position = pos;
            // CpuMoveState follows transform.right. Reverse the root facing if the
            // scene spawner rotation differs from this trial's actual opponent side.
            if (go.transform.right.x * forwardSign < 0f)
                go.transform.rotation = Quaternion.Euler(0f, 180f, 0f) * go.transform.rotation;
            Stats stats = go.GetComponent<Stats>();
            if (stats == null) return false;
            _units.Add(new ObservedUnit { go = go, stats = stats, teamA = isA,
                startingPosition = pos, initialHp = Mathf.Max(0, stats._CurrentHealth),
                previousHp = Mathf.Max(0, stats._CurrentHealth), forwardSign = forwardSign });
        }
        return true;
    }

    void SampleDamage(TrialResult trial, float elapsed)
    {
        foreach (ObservedUnit u in _units)
        {
            float hp = u.go == null || u.stats == null ? 0f : Mathf.Max(0f, u.stats._CurrentHealth);
            float lost = Mathf.Max(0f, u.previousHp - hp);
            if (lost > 0f)
            {
                u.totalDamageReceived += lost;
                if (trial.firstHitSeconds < 0f) trial.firstHitSeconds = elapsed;
            }
            u.previousHp = hp;
        }
    }

    int CountObserved(bool a) { int n = 0; foreach (ObservedUnit u in _units) if (u.teamA == a) n++; return n; }
    int CountAlive(bool a)
    {
        int n = 0;
        foreach (ObservedUnit u in _units)
            if (u.teamA == a && u.go != null && u.stats != null && !u.stats._IsDead && u.stats._CurrentHealth > 0f) n++;
        return n;
    }
    float InitialHp(bool a) { float sum = 0; foreach (ObservedUnit u in _units) if (u.teamA == a) sum += u.initialHp; return sum; }
    float RemainingHp(bool a)
    {
        float sum = 0;
        foreach (ObservedUnit u in _units)
            if (u.teamA == a && u.go != null && u.stats != null && !u.stats._IsDead) sum += Mathf.Max(0, u.stats._CurrentHealth);
        return sum;
    }
    float TotalDamageReceived(bool a) { float sum = 0; foreach (ObservedUnit u in _units) if (u.teamA == a) sum += u.totalDamageReceived; return sum; }
    float AverageAdvance(bool a)
    {
        float total = 0f; int n = 0;
        foreach (ObservedUnit u in _units)
        {
            if (u.teamA != a) continue;
            n++;
            if (u.go != null) total += (u.go.transform.position.x - u.startingPosition.x) * u.forwardSign;
        }
        return n > 0 ? total / n : 0f;
    }
    bool HasUntrackedLivingUnits()
    {
        Stats[] existing = FindObjectsOfType<Stats>();
        foreach (Stats s in existing)
            if (s != null && s.gameObject.activeInHierarchy && !s._IsDead && s._CurrentHealth > 0f && !_spawned.Contains(s.gameObject))
                return true;
        return false;
    }
    void CleanupSpawned()
    {
        foreach (GameObject go in _spawned) if (go != null) { go.SetActive(false); Destroy(go); }
        _spawned.Clear(); _units.Clear();
    }
    void SuspendBehaviours()
    {
        _suspended.Clear(); _previousEnabled.Clear();
        foreach (MonoBehaviour b in suspendDuringSuite)
        {
            if (b == null || b == this || b is SpawnObjects || b is Stats || b is CpuStateManager) continue;
            if (_suspended.Contains(b)) continue;
            _suspended.Add(b); _previousEnabled.Add(b.enabled); b.enabled = false;
        }
    }
    void RestoreBehaviours()
    {
        for (int i = 0; i < _suspended.Count; i++) if (_suspended[i] != null) _suspended[i].enabled = _previousEnabled[i];
        _suspended.Clear(); _previousEnabled.Clear();
    }
    void FinishSuite(string finalStatus, bool export)
    {
        CleanupSpawned();
        RestoreBehaviours();
        if (_ownsTimeScale) Time.timeScale = _previousTimeScale;
        _ownsTimeScale = false;
        isRunning = false;
        _runningCoroutine = null;
        status = finalStatus;
        if (export && trialResults.Count > 0) ExportCSV();
        Debug.Log("[ArmyValidation] " + status, this);
    }

    ScenarioSummary Summarize(Scenario s, int index)
    {
        var summary = new ScenarioSummary { scenarioId = s.scenarioId, category = s.generatedCategory, scenarioIndex = index, prediction = Predict(s) };
        foreach (TrialResult r in trialResults)
        {
            if (r.scenarioIndex != index) continue;
            if (r.actualSide == "Invalid") { summary.invalid++; continue; }
            summary.completed++;
            if (r.actualSide == "TeamA") summary.aWins++;
            else if (r.actualSide == "TeamB") summary.bWins++;
            else if (r.actualSide == "Draw") summary.draws++;
            else summary.timeouts++;
            summary.averageDuration += r.duration;
            summary.averageAHealthFraction += r.aRemainingHp / Mathf.Max(1f, r.aInitialHp);
            summary.averageBHealthFraction += r.bRemainingHp / Mathf.Max(1f, r.bInitialHp);
            if (r.actualSide == "TeamA" || r.actualSide == "TeamB")
            {
                summary.decisiveTrials++;
                if (r.actualSide == r.predictedSide) summary.predictedSideCorrect++;
            }
        }
        if (summary.completed > 0)
        {
            summary.averageDuration /= summary.completed;
            summary.averageAHealthFraction /= summary.completed;
            summary.averageBHealthFraction /= summary.completed;
        }
        return summary;
    }

    static string F(float value) { return value.ToString("R", CultureInfo.InvariantCulture); }
    static string Csv(string value) { return "\"" + (value ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\""; }

    [ContextMenu("Export Validation CSV Files")]
    public void ExportCSV()
    {
        string prefix = string.IsNullOrEmpty(outputPrefix) ? "ArmyCombatValidation" : outputPrefix;
        foreach (char c in Path.GetInvalidFileNameChars()) prefix = prefix.Replace(c, '_');
        if (string.IsNullOrEmpty(_suiteId)) _suiteId = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string path = Application.persistentDataPath;
        try
        {
            var scenariosCsv = new StringBuilder("suite_id,scenario_index,scenario_id,team_a,team_b,formation_a,formation_b,separation,spacing,row_spacing,max_per_row,repetitions,reverse_orientation,duration_limit,predicted_unweighted_outcome,predicted_side,source_mode,category,generation_seed,team_a_cost,team_b_cost\n");
            for (int i = 0; i < _suiteScenarios.Count; i++)
            {
                Scenario s = _suiteScenarios[i]; float prediction = Predict(s);
                scenariosCsv.Append(Csv(_suiteId)).Append(',').Append(i).Append(',').Append(Csv(s.scenarioId)).Append(',')
                    .Append(Csv(Definition(s.teamA))).Append(',').Append(Csv(Definition(s.teamB))).Append(',')
                    .Append(s.formationA).Append(',').Append(s.formationB).Append(',').Append(F(s.separation)).Append(',')
                    .Append(F(s.unitSpacing)).Append(',').Append(F(s.rowSpacing)).Append(',').Append(s.maxPerRow).Append(',')
                    .Append(s.repetitions).Append(',').Append(s.reverseOrientation).Append(',').Append(F(s.durationSeconds)).Append(',')
                    .Append(F(prediction)).Append(',').Append(prediction > 0.0001f ? "TeamA" : prediction < -0.0001f ? "TeamB" : "Draw")
                    .Append(',').Append(_activeMode).Append(',').Append(Csv(s.generatedCategory)).Append(',').Append(_activeSeed)
                    .Append(',').Append(ArmyCost(s.teamA)).Append(',').Append(ArmyCost(s.teamB)).AppendLine();
            }
            var trialsCsv = new StringBuilder("suite_id,scenario_index,scenario_id,repeat,orientation,team_a,team_b,formation_a,formation_b,separation,spacing,duration_limit,predicted_unweighted_outcome,predicted_side,actual_side,duration,a_initial,b_initial,a_survivors,b_survivors,a_initial_hp,b_initial_hp,a_remaining_hp,b_remaining_hp,a_damage_received,b_damage_received,a_mean_advance,b_mean_advance,first_hit_seconds,note,category\n");
            foreach (TrialResult r in trialResults)
                trialsCsv.Append(Csv(_suiteId)).Append(',').Append(r.scenarioIndex).Append(',').Append(Csv(r.scenarioId)).Append(',').Append(r.repeat).Append(',').Append(r.orientation).Append(',')
                    .Append(Csv(r.teamADefinition)).Append(',').Append(Csv(r.teamBDefinition)).Append(',').Append(r.formationA).Append(',').Append(r.formationB).Append(',')
                    .Append(F(r.separation)).Append(',').Append(F(r.spacing)).Append(',').Append(F(r.durationLimit)).Append(',').Append(F(r.predictedOutcome)).Append(',')
                    .Append(r.predictedSide).Append(',').Append(r.actualSide).Append(',').Append(F(r.duration)).Append(',')
                    .Append(r.aInitial).Append(',').Append(r.bInitial).Append(',').Append(r.aSurvivors).Append(',').Append(r.bSurvivors).Append(',')
                    .Append(F(r.aInitialHp)).Append(',').Append(F(r.bInitialHp)).Append(',').Append(F(r.aRemainingHp)).Append(',').Append(F(r.bRemainingHp)).Append(',')
                    .Append(F(r.aDamageReceived)).Append(',').Append(F(r.bDamageReceived)).Append(',').Append(F(r.aAdvanceMeters)).Append(',').Append(F(r.bAdvanceMeters)).Append(',')
                    .Append(F(r.firstHitSeconds)).Append(',').Append(Csv(r.note)).Append(',').Append(Csv(r.category)).AppendLine();
            var summaryCsv = new StringBuilder("suite_id,scenario_index,scenario_id,predicted_unweighted_outcome,completed,invalid,a_wins,b_wins,draws,timeouts,decisive_trials,predicted_correct,accuracy_decisive,average_duration,average_a_hp_fraction,average_b_hp_fraction,category\n");
            foreach (ScenarioSummary s in summaries)
                summaryCsv.Append(Csv(_suiteId)).Append(',').Append(s.scenarioIndex).Append(',').Append(Csv(s.scenarioId)).Append(',')
                    .Append(F(s.prediction)).Append(',').Append(s.completed).Append(',').Append(s.invalid).Append(',')
                    .Append(s.aWins).Append(',').Append(s.bWins).Append(',').Append(s.draws).Append(',').Append(s.timeouts).Append(',')
                    .Append(s.decisiveTrials).Append(',').Append(s.predictedSideCorrect).Append(',')
                    .Append(s.decisiveTrials > 0 ? F((float)s.predictedSideCorrect / s.decisiveTrials) : "").Append(',')
                    .Append(F(s.averageDuration)).Append(',').Append(F(s.averageAHealthFraction)).Append(',').Append(F(s.averageBHealthFraction)).Append(',').Append(Csv(s.category)).AppendLine();
            File.WriteAllText(Path.Combine(path, prefix + "_" + _suiteId + "_scenarios.csv"), scenariosCsv.ToString(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(path, prefix + "_" + _suiteId + "_trials.csv"), trialsCsv.ToString(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(path, prefix + "_" + _suiteId + "_summary.csv"), summaryCsv.ToString(), Encoding.UTF8);
            Debug.Log("[ArmyValidation] Exported 3 CSVs to " + path, this);
#if UNITY_EDITOR
            EditorUtility.RevealInFinder(path);
#endif
        }
        catch (Exception ex) { Debug.LogError("[ArmyValidation] CSV export failed: " + ex, this); }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ArmyCombatValidationRunner))]
public class ArmyCombatValidationRunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var runner = (ArmyCombatValidationRunner)target;
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!Application.isPlaying || runner.isRunning))
            if (GUILayout.Button("Run Controlled Army Matchups", GUILayout.Height(32))) runner.RunSuite();
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !runner.isRunning))
            if (GUILayout.Button("Stop", GUILayout.Height(24))) runner.StopSuite();
        EditorGUILayout.Space();
        if (GUILayout.Button("Preview / Regenerate Random Matchups", GUILayout.Height(26)))
        {
            runner.RegeneratePreview();
            EditorUtility.SetDirty(runner);
        }
        if (GUILayout.Button("Copy Selected Preview Matchup To Manual Scenarios", GUILayout.Height(26)))
        {
            Undo.RecordObject(runner, "Copy generated matchup to manual scenarios");
            runner.PromotePreviewScenario();
            EditorUtility.SetDirty(runner);
        }
        if (GUILayout.Button("Export CSVs", GUILayout.Height(24))) runner.ExportCSV();
    }
}
#endif
