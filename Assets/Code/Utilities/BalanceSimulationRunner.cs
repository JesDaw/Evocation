using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BalanceSimulationRunner : MonoBehaviour
{
    [Header("1. Spawner References")]
    [Tooltip("The Allied SpawnObjects component (spawns test characters on player side).")]
    public SpawnObjects alliedSpawner;

    [Tooltip("The Enemy SpawnObjects component (spawns Anchor units on enemy side).")]
    public SpawnObjects enemySpawner;

    [Header("2. Game Roster")]
    [Tooltip("The fixed baseline unit every character is compared against (100% baseline).")]
    public ScriptableStats anchorUnit;

    [Tooltip("All ClanStats assets in the game.")]
    public ClanStats[] all_clan_stats;

    [Tooltip("Optional individual characters to test in addition to the clans above.")]
    public List<ScriptableStats> extraUnitsToTest = new List<ScriptableStats>();

    [Header("3. Cost & Power Scaling")]
    [Tooltip("Scales simulated Anchor-Equivalents into Spawn Cost units. If 0, uses AnchorUnit._spawnCost.")]
    public float costPerAnchorEquivalent = 0f;

    [Tooltip("Automatically writes formula-predicted power and value discrepancies into each ScriptableStats asset when finished.")]
    public bool autoUpdateScriptableStats = true;

    [Header("4. Synthetic Model Training")]
    [Tooltip("Generate randomized multi-stat synthetic units to train the formula.")]
    public bool includeSyntheticTrials = true;
    [Min(12)] public int syntheticTrainingUnitCount = 45;
    [Tooltip("Seed used to generate reproducible synthetic stat combinations.")]
    public int syntheticRandomSeed = 1729;
    [Tooltip("Seed used for trial conditions. Every tested unit receives the same jitter schedule for fair comparison.")]
    public int trialJitterSeed = 271828;

    [Tooltip("How many times to test each character across jittered waves.")]
    [Range(1, 12)] public int repeatsPerUnit = 2;

    [Header("5. Progressive Wave Gauntlet & Jitter")]
    [Tooltip("Base distance (meters) ahead of the candidate where each new Anchor wave spawns.")]
    [Min(6f)] public float waveReinforcementDistanceMeters = 14f;
    [Tooltip("How much tougher each subsequent Anchor wave gets (e.g. 0.18 = +18% HP, Atk, and KBH per wave). Prevents infinite 1-hit stunlocks and ensures every unit is tested to destruction!")]
    [Range(0.05f, 0.40f)] public float waveEscalationPerWave = 0.18f;
    [Tooltip("Maximum random variation added to the distance of each Anchor wave.")]
    [Min(0f)] public float anchorSpawnDistanceJitterMeters = 3.5f;
    [Tooltip("Jitters the Anchor's HP, KBH, and KBD by +/- this fraction on each wave.")]
    [Range(0.05f, 0.45f)] public float anchorStatJitterFraction = 0.20f;
    [Tooltip("Maximum randomized delay before the initial Anchor appears.")]
    [Min(0f)] public float anchorSpawnDelayJitterSeconds = 0.30f;
    [Tooltip("Chance that a wave uses a longer approach distance so movement speed is tested over longer marches.")]
    [Range(0f, 1f)] public float farSpawnTrialChance = 0.25f;
    [Tooltip("Additional separation used for far-approach waves, in world units.")]
    [Min(0f)] public float farSpawnExtraMeters = 12f;

    [Header("6. Battle Rules & Speed Optimization")]
    [Range(1f, 15f)] public float simulationTimeScale = 10f;
    [Tooltip("Maximum simulation seconds per trial.")]
    [Min(10f)] public float maxTrialDuration = 40f;
    [Tooltip("Maximum Anchor waves per trial.")]
    [Min(3)] public int maxAnchorsPerTrial = 15;
    [Tooltip("Extra space beyond the longest hostile action's actual forward detection reach when placing a wave.")]
    [Min(0f)] public float spawnDistanceMargin = 2.0f;
    [Tooltip("Weight given to Frontline Advance Velocity (meters claimed per second) so faster units are rewarded for map control without needing a 60s timer cutoff.")]
    [Range(0.05f, 1.0f)] public float frontlineVelocityWeight = 0.25f;
    [Tooltip("Run each matchup with the test unit on both sides and average the results.")]
    public bool testBothOrientations = false;

    [HideInInspector] public float territoryGroundWeightPer10Meters = 0.30f;
    [HideInInspector] public float anchorHpJitterFraction = 0.20f;
    [HideInInspector] public float combatEngagementDistance = 0f;
    [HideInInspector] public float anchorStreamInterval = 0f;
    [HideInInspector] public float territoryScoreScaleMeters = 5f;

    [Header("7. Live Progress & Real-Time Timer (Read-Only)")]
    public bool isRunning = false;
    public string currentStatus = "Idle";
    public float realTimeElapsedSeconds = 0f;
    [Tooltip("The Anchor mirror's raw push score. Used to normalize scores so the Anchor is the 1.0x reference.")]
    public float anchorMirrorBaselineScore = 1f;
    [Tooltip("Held-out R-squared for real roster units compared with formula predictions.")]
    public float validationRSquared = 0f;
    public List<TrialResult> results = new List<TrialResult>();

    [Header("8. Solved Cobb-Douglas Elasticities (Exponents)")]
    [Tooltip("Solved multiplicative elasticities.")]
    public CobbDouglasElasticities elasticities;

    [Header("9. Configuration & Export")]
    public BalanceConfig balanceConfig;
    public string diagnosticCsvFileName = "BalanceSimulationDiagnostics.csv";

    List<GameObject> _activeSpawnedObjects = new List<GameObject>();
    System.Diagnostics.Stopwatch _realTimeStopwatch = new System.Diagnostics.Stopwatch();
    System.Random _syntheticRandom;
    System.Random _trialRandom;
    float _originalTimeScale = 1f;
    bool _hasOriginalTimeScale = false;

    [System.Serializable]
    public struct TrialResult
    {
        public string unitName;
        public string clanName;
        public bool isSynthetic;
        public ScriptableStats statsAsset;

        public int spawnCost;
        public float rawHP;
        public float rawAtk;
        public float rawCycleSec;
        public float rawFreq;
        public float rawDPS;
        public float rawRange;
        public float rawMove;
        public float rawKBD;
        public float rawKBDPS;
        public float rawKBH;
        public int maxTargets;
        public float utilityPower;

        public float hpRatio;
        public float dpsRatio;
        public float rangeRatio;
        public float moveRatio;
        public float kbdRatio;
        public float kbhRatio;

        public float anchorsKilledEquivalent;
        public float timeSurvived;
        public float normalizedLanePush;
        public float compositePowerScore;
        public float scaledCalculatedPower;
        public float formulaPredictedPower;

        public float averageTerritoryShiftMeters;
        public float finalTerritoryShiftMeters;
        public float averageCandidateAdvanceMeters;
        public float averageOpponentAdvanceMeters;
        public float averageDamageDealt;
        public float averageDamageReceived;
        public float averageOpponentSurvivalSeconds;
        public float candidateDeathRate;
        public float opponentDeathRate;
    }

    struct TrialSample
    {
        public float averageTerritoryShiftMeters;
        public float finalTerritoryShiftMeters;
        public float candidateAdvanceMeters;
        public float opponentAdvanceMeters;
        public float candidateSurvivalSeconds;
        public float opponentSurvivalSeconds;
        public float effectiveDamageDealt;
        public float damageReceived;
        public float remainingCandidateHpRatio;
        public int anchorsDefeatedCount;
        public bool candidateDied;
        public bool opponentDied;
    }

    [System.Serializable]
    public struct CobbDouglasElasticities
    {
        [Tooltip("Elasticity exponent for MaxHealth. 1.0 means +10% HP = +10% Power.")]
        public float e_Health;

        [Tooltip("Elasticity exponent for Effective Combat DPS.")]
        public float e_DPS;

        [Tooltip("Elasticity exponent for Horizontal Range.")]
        public float e_Range;

        [Tooltip("Elasticity exponent for Move Speed.")]
        public float e_MoveSpeed;

        [Tooltip("Elasticity exponent for KnockBack Power.")]
        public float e_KBD;

        [Tooltip("Elasticity exponent for KnockBack Max Health (Poise).")]
        public float e_KBH;

        [Tooltip("Overall scaling multiplier (1.0 when normalized to Anchor).")]
        public float scaleConstant;

        [Tooltip("Goodness of fit (0 to 1) on the training set.")]
        public float rSquared;
    }

    [ContextMenu("Auto-Grab References from Project")]
    public void AutoWireReferences()
    {
        if (alliedSpawner == null) alliedSpawner = SpawnObjects.PlayerInstance;
        if (enemySpawner == null) enemySpawner = SpawnObjects.EnemyInstance;

        if (all_clan_stats == null || all_clan_stats.Length == 0)
        {
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:ClanStats");
            List<ClanStats> clans = new List<ClanStats>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ClanStats c = AssetDatabase.LoadAssetAtPath<ClanStats>(path);
                if (c != null && !clans.Contains(c)) clans.Add(c);
            }
            all_clan_stats = clans.ToArray();
#endif
        }
        if (balanceConfig == null)
        {
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:BalanceConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                balanceConfig = AssetDatabase.LoadAssetAtPath<BalanceConfig>(path);
            }
#endif
        }
    }

    public void StartSimulationSuite()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[BalanceSim] Enter PLAY MODE first to run the simulation!");
            return;
        }
        if (isRunning) return;
        StartCoroutine(RunSuiteCoroutine());
    }

    public void StopSimulationSuite()
    {
        if (!isRunning) return;
        StopAllCoroutines();
        _realTimeStopwatch.Stop();
        CleanupTrialObjects();
        if (_hasOriginalTimeScale) Time.timeScale = _originalTimeScale;
        _hasOriginalTimeScale = false;
        isRunning = false;
        currentStatus = $"Stopped by user at {realTimeElapsedSeconds:F1}s real time.";
    }

    List<ScriptableStats> CreateRandomizedSyntheticTrainingUnits()
    {
        List<ScriptableStats> clones = new List<ScriptableStats>();
        if (anchorUnit == null) return clones;

        _syntheticRandom = new System.Random(syntheticRandomSeed);
        int count = Mathf.Max(16, syntheticTrainingUnitCount);

        for (int i = 0; i < count; i++)
        {
            ScriptableStats clone = Instantiate(anchorUnit);
            clone.name = $"Synthetic_{i + 1:000}";
            if (anchorUnit.combatActions != null)
                clone.combatActions = new List<CombatAction>(anchorUnit.combatActions);

            float hpMult       = RandomLogMultiplier(_syntheticRandom, 0.45f, 2.6f);
            float attackMult   = RandomLogMultiplier(_syntheticRandom, 0.50f, 3.0f);
            float cooldownMult = RandomLogMultiplier(_syntheticRandom, 0.70f, 2.2f);
            // Bias 40% of synthetic units to stay near melee range (0.9x - 1.4x Anchor range) so melee KBH poise is thoroughly tested!
            float rangeMult    = _syntheticRandom.NextDouble() < 0.40
                ? RandomLogMultiplier(_syntheticRandom, 0.90f, 1.45f)
                : RandomLogMultiplier(_syntheticRandom, 1.45f, 4.5f);
            float moveMult     = RandomLogMultiplier(_syntheticRandom, 0.55f, 2.4f);
            float kbDamageMult = _syntheticRandom.NextDouble() < 0.15
                ? 0f
                : RandomLogMultiplier(_syntheticRandom, 0.30f, 2.8f);
            float kbHealthMult = RandomLogMultiplier(_syntheticRandom, 0.45f, 3.0f);

            clone._MaxHealth          = Mathf.Max(10, Mathf.RoundToInt(anchorUnit._MaxHealth * hpMult));
            clone._AttackDamage       = Mathf.Max(3, Mathf.RoundToInt(anchorUnit._AttackDamage * attackMult));
            clone._ActionCooldown     = Mathf.Max(0.2f, anchorUnit._ActionCooldown * cooldownMult);
            // Do NOT clamp _HorizontalRange to 1.5f (which previously forced StoppingRange >= 3.38m when extendsForward=true!)
            clone._HorizontalRange    = Mathf.Max(0.5f, anchorUnit._HorizontalRange * rangeMult);
            clone._MoveSpeed          = Mathf.Max(0.8f, anchorUnit._MoveSpeed * moveMult);
            clone._KnockBackDamage    = Mathf.Max(0f, anchorUnit._KnockBackDamage * kbDamageMult);
            clone._KnockBackMaxHealth = Mathf.Max(5f, anchorUnit._KnockBackMaxHealth * kbHealthMult);

            clones.Add(clone);
        }

        return clones;
    }

    float RandomLogMultiplier(System.Random random, float min, float max)
    {
        if (random == null) return Mathf.Sqrt(min * max);
        float t = (float)random.NextDouble();
        return Mathf.Exp(Mathf.Lerp(Mathf.Log(min), Mathf.Log(max), t));
    }

    float RandomRange(float min, float max)
    {
        if (max <= min) return min;
        if (_trialRandom == null) return UnityEngine.Random.Range(min, max);
        return min + (float)_trialRandom.NextDouble() * (max - min);
    }

    /// <summary>
    /// Computes the buffered Knockback Ratio combining per-hit KBD (which determines 1-hit stunlock thresholds
    /// like WolfRider's 30 KBD) and KBDPS (sustained poise pressure), with a 0.50 floor for 0-KBD units (WolfMage).
    /// </summary>
    public static float ComputeBufferedKbdRatio(float unitKBD, float unitKBDPS, float anchorKBD, float anchorKBDPS)
    {
        float perHitRatio = unitKBD / Mathf.Max(anchorKBD, 0.1f);
        float rateRatio   = unitKBDPS / Mathf.Max(anchorKBDPS, 0.1f);
        // 65% weight on per-hit KBD (stunlock power) + 35% weight on sustained KBDPS
        float combinedKbdRatio = (perHitRatio * 0.65f) + (rateRatio * 0.35f);
        return Mathf.Max((1f + combinedKbdRatio) * 0.5f, 0.25f);
    }

    IEnumerator RunSuiteCoroutine()
    {
        isRunning = true;
        results.Clear();
        AutoWireReferences();

        if (anchorUnit == null || alliedSpawner == null || enemySpawner == null)
        {
            Debug.LogError("[BalanceSim] Missing AnchorUnit, AlliedSpawner, or EnemySpawner!");
            isRunning = false;
            yield break;
        }

        _realTimeStopwatch.Restart();
        realTimeElapsedSeconds = 0f;
        _originalTimeScale = Time.timeScale;
        _hasOriginalTimeScale = true;

        List<ScriptableStats> testPool = new List<ScriptableStats> { anchorUnit };
        foreach (ScriptableStats unit in GatherAllRosterUnits())
            if (unit != null && !testPool.Contains(unit)) testPool.Add(unit);

        List<ScriptableStats> tempClonesToDestroy = new List<ScriptableStats>();
        if (includeSyntheticTrials)
        {
            tempClonesToDestroy = CreateRandomizedSyntheticTrainingUnits();
            testPool.AddRange(tempClonesToDestroy);
        }

        Time.timeScale = simulationTimeScale;
        float anchorCostScale = GetEffectiveAnchorCostScale();

        PowerMath.UnitProfile anchorProfile = PowerMath.GetProfile(anchorUnit);
        float anchorCycle = GetEffectiveCombatCycleSeconds(anchorUnit);
        float anchorFreq = 1f / Mathf.Max(anchorCycle, 0.05f);
        float anchorDPS = Mathf.Max(anchorProfile.Atk * anchorFreq, 0.01f);
        float anchorRange = Mathf.Max(GetTrueStoppingRange(anchorUnit), 0.1f);
        float anchorMove = Mathf.Max(anchorProfile.Move, 0.1f);
        float anchorKBD = Mathf.Max(anchorProfile.KBD, 0.1f);
        float anchorKBDPS = Mathf.Max(anchorProfile.KBD * anchorFreq, 0.01f);
        float anchorKBH = Mathf.Max(anchorProfile.KBH, 0.1f);
        float anchorHP = Mathf.Max(anchorProfile.HP, 0.1f);

        anchorMirrorBaselineScore = 1f;
        int orientationCount = testBothOrientations ? 2 : 1;
        int repeats = Mathf.Max(1, repeatsPerUnit);

        for (int i = 0; i < testPool.Count; i++)
        {
            ScriptableStats candidate = testPool[i];
            _trialRandom = new System.Random(trialJitterSeed);
            List<TrialSample> samples = new List<TrialSample>();

            for (int repeat = 0; repeat < repeats; repeat++)
            {
                for (int orientation = 0; orientation < orientationCount; orientation++)
                {
                    realTimeElapsedSeconds = (float)_realTimeStopwatch.Elapsed.TotalSeconds;
                    currentStatus = $"[{realTimeElapsedSeconds:F1}s] Testing [{i + 1}/{testPool.Count}] {candidate.name} (Run {repeat + 1}/{repeats})";

                    TrialSample sample = default;
                    yield return StartCoroutine(RunSingleTrial(candidate, orientation == 1, result => sample = result));
                    samples.Add(sample);
                }
            }

            float avgTerritory = AverageSamples(samples, x => x.averageTerritoryShiftMeters);
            float avgFinalTerritory = AverageSamples(samples, x => x.finalTerritoryShiftMeters);
            float avgCandidateAdvance = AverageSamples(samples, x => x.candidateAdvanceMeters);
            float avgOpponentAdvance = AverageSamples(samples, x => x.opponentAdvanceMeters);
            float avgSurvival = AverageSamples(samples, x => x.candidateSurvivalSeconds);
            float avgOpponentSurvival = AverageSamples(samples, x => x.opponentSurvivalSeconds);
            float avgDamageDealt = AverageSamples(samples, x => x.effectiveDamageDealt);
            float avgDamageReceived = AverageSamples(samples, x => x.damageReceived);
            float avgRemainingHpRatio = AverageSamples(samples, x => x.remainingCandidateHpRatio);
            float candidateDeathRate = samples.Count == 0 ? 0f : CountSamples(samples, x => x.candidateDied) / (float)samples.Count;
            float opponentDeathRate = samples.Count == 0 ? 0f : CountSamples(samples, x => x.opponentDied) / (float)samples.Count;

            PowerMath.UnitProfile profile = PowerMath.GetProfile(candidate);
            float effCycleSec = GetEffectiveCombatCycleSeconds(candidate);
            float freq = 1f / Mathf.Max(effCycleSec, 0.05f);
            float dps = profile.Atk * freq;
            float range = GetTrueStoppingRange(candidate);
            float kbdps = profile.KBD * freq;
            float utilityBonus = PowerMath.GetUtilityPower(candidate);

            float enduranceBonus = 1f + (Mathf.Clamp01(avgRemainingHpRatio) * 0.50f);
            float combatAnchorEquivalents = (avgDamageDealt / anchorHP) * enduranceBonus;

            // Measure both total ground claimed and frontline advance velocity (m/s) so MoveSpeed
            // directly rewards units that push territory quickly, even on short trials!
            float advanceVelocity = avgCandidateAdvance / Mathf.Max(avgSurvival, 2f);
            float velocityMultiplier = 1f + Mathf.Clamp(advanceVelocity / Mathf.Max(anchorMove, 0.5f), -0.2f, 1.5f) * frontlineVelocityWeight;

            float rawPushPowerScore = Mathf.Max(0.05f, combatAnchorEquivalents * velocityMultiplier);

            if (i == 0 && candidate == anchorUnit)
                anchorMirrorBaselineScore = Mathf.Max(rawPushPowerScore, 0.05f);

            float normalizedPowerRatio = rawPushPowerScore / anchorMirrorBaselineScore;
            bool isTempClone = tempClonesToDestroy.Contains(candidate);

            float bufferedKbdRatio = ComputeBufferedKbdRatio(profile.KBD, kbdps, anchorKBD, anchorKBDPS);

            results.Add(new TrialResult
            {
                unitName = candidate.name,
                clanName = isTempClone ? "SyntheticTraining" : FindClanNameForUnit(candidate),
                isSynthetic = isTempClone,
                statsAsset = isTempClone ? null : candidate,
                spawnCost = candidate._spawnCost,
                rawHP = profile.HP,
                rawAtk = profile.Atk,
                rawCycleSec = effCycleSec,
                rawFreq = freq,
                rawDPS = dps,
                rawRange = range,
                rawMove = profile.Move,
                rawKBD = profile.KBD,
                rawKBDPS = kbdps,
                rawKBH = profile.KBH,
                maxTargets = profile.MaxTargets,
                utilityPower = utilityBonus,
                hpRatio = profile.HP / anchorHP,
                dpsRatio = dps / anchorDPS,
                rangeRatio = range / anchorRange,
                moveRatio = profile.Move / anchorMove,
                kbdRatio = bufferedKbdRatio,
                kbhRatio = profile.KBH / anchorKBH,
                anchorsKilledEquivalent = combatAnchorEquivalents,
                timeSurvived = avgSurvival,
                normalizedLanePush = avgFinalTerritory,
                compositePowerScore = normalizedPowerRatio,
                scaledCalculatedPower = (normalizedPowerRatio * anchorCostScale) + utilityBonus,
                formulaPredictedPower = 0f,
                averageTerritoryShiftMeters = avgTerritory,
                finalTerritoryShiftMeters = avgFinalTerritory,
                averageCandidateAdvanceMeters = avgCandidateAdvance,
                averageOpponentAdvanceMeters = avgOpponentAdvance,
                averageDamageDealt = avgDamageDealt,
                averageDamageReceived = avgDamageReceived,
                averageOpponentSurvivalSeconds = avgOpponentSurvival,
                candidateDeathRate = candidateDeathRate,
                opponentDeathRate = opponentDeathRate
            });
        }

        Time.timeScale = _originalTimeScale;
        _hasOriginalTimeScale = false;
        _realTimeStopwatch.Stop();
        realTimeElapsedSeconds = (float)_realTimeStopwatch.Elapsed.TotalSeconds;
        CleanupTrialObjects();
        foreach (ScriptableStats clone in tempClonesToDestroy) if (clone != null) Destroy(clone);

        SolveCobbDouglasRegression();
        for (int i = 0; i < results.Count; i++)
        {
            TrialResult r = results[i];
            float predRatio = elasticities.scaleConstant
                * Mathf.Pow(Mathf.Max(r.hpRatio, 0.05f), elasticities.e_Health)
                * Mathf.Pow(Mathf.Max(r.dpsRatio, 0.05f), elasticities.e_DPS)
                * Mathf.Pow(Mathf.Max(r.rangeRatio, 0.05f), elasticities.e_Range)
                * Mathf.Pow(Mathf.Max(r.moveRatio, 0.05f), elasticities.e_MoveSpeed)
                * Mathf.Pow(Mathf.Max(r.kbdRatio, 0.25f), elasticities.e_KBD)
                * Mathf.Pow(Mathf.Max(r.kbhRatio, 0.05f), elasticities.e_KBH);
            r.formulaPredictedPower = (predRatio * anchorCostScale) + r.utilityPower;
            results[i] = r;
        }

        CalculateHeldOutValidationRSquared();
        if (autoUpdateScriptableStats) UpdateAllScriptableStatsAndClans();
        currentStatus = $"Done in {realTimeElapsedSeconds:F1}s real time! (Synthetic R² = {elasticities.rSquared:F3}, Real Roster Validation R² = {validationRSquared:F3})";
        Debug.Log($"<b>[BalanceSim] {currentStatus}</b>");
        isRunning = false;
    }

    void CalculateHeldOutValidationRSquared()
    {
        List<TrialResult> validationResults = new List<TrialResult>();
        foreach (TrialResult result in results)
        {
            if (!result.isSynthetic && result.statsAsset != null && result.statsAsset != anchorUnit)
                validationResults.Add(result);
        }

        if (validationResults.Count < 2)
        {
            validationRSquared = 0f;
            return;
        }

        double meanObserved = 0d;
        foreach (TrialResult result in validationResults)
            meanObserved += System.Math.Log(System.Math.Max(result.scaledCalculatedPower, 1f));
        meanObserved /= validationResults.Count;

        double ssResidual = 0d;
        double ssTotal = 0d;
        foreach (TrialResult result in validationResults)
        {
            double obsLog = System.Math.Log(System.Math.Max(result.scaledCalculatedPower, 1f));
            double predLog = System.Math.Log(System.Math.Max(result.formulaPredictedPower, 1f));
            double error = obsLog - predLog;
            double deviation = obsLog - meanObserved;
            ssResidual += error * error;
            ssTotal += deviation * deviation;
        }

        validationRSquared = ssTotal > 1e-6 ? (float)(1d - ssResidual / ssTotal) : 0f;
    }

    float AverageSamples(List<TrialSample> samples, System.Func<TrialSample, float> selector)
    {
        if (samples == null || samples.Count == 0) return 0f;
        List<float> values = new List<float>(samples.Count);
        foreach (TrialSample sample in samples) values.Add(selector(sample));
        return GetOutlierResistantAverage(values);
    }

    int CountSamples(List<TrialSample> samples, System.Func<TrialSample, bool> selector)
    {
        int count = 0;
        foreach (TrialSample sample in samples) if (selector(sample)) count++;
        return count;
    }

    [ContextMenu("Export Diagnostic CSV Report")]
    public void ExportDiagnosticCSV()
    {
        AutoWireReferences();
        CultureInfo inv = CultureInfo.InvariantCulture;
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("=== TERRITORY-CONTROL BALANCE SIMULATION ===");
        csv.AppendLine("Parameter,Value");
        csv.AppendLine($"Export Timestamp,\"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\"");
        csv.AppendLine($"Real-World Run Time (s),{realTimeElapsedSeconds.ToString("F2", inv)}");
        csv.AppendLine($"Anchor Unit,\"{(anchorUnit != null ? anchorUnit.name : "None")}\"");
        csv.AppendLine($"Effective Cost Per Anchor Equivalent,{GetEffectiveAnchorCostScale().ToString("F2", inv)}");
        csv.AppendLine($"Anchor Mirror Baseline Score,{anchorMirrorBaselineScore.ToString("F3", inv)}");
        csv.AppendLine($"Repeats Per Unit,{repeatsPerUnit}");
        csv.AppendLine($"Synthetic Training Enabled,{includeSyntheticTrials}");
        csv.AppendLine($"Synthetic Training Unit Count,{syntheticTrainingUnitCount}");
        csv.AppendLine($"Wave Reinforcement Distance (m),{waveReinforcementDistanceMeters.ToString("F2", inv)}");
        csv.AppendLine($"Wave Escalation Per Wave,{waveEscalationPerWave.ToString("F2", inv)}");
        csv.AppendLine($"Anchor Spawn Distance Jitter (m),{anchorSpawnDistanceJitterMeters.ToString("F2", inv)}");
        csv.AppendLine($"Anchor Stat Jitter Fraction,{anchorStatJitterFraction.ToString("F2", inv)}");
        csv.AppendLine($"Far Spawn Trial Chance,{farSpawnTrialChance.ToString("F2", inv)}");
        csv.AppendLine($"Far Spawn Extra Distance (m),{farSpawnExtraMeters.ToString("F2", inv)}");
        csv.AppendLine($"Maximum Trial Duration (s),{maxTrialDuration.ToString("F2", inv)}");
        csv.AppendLine($"Max Anchors Per Trial,{maxAnchorsPerTrial}");
        csv.AppendLine($"Frontline Velocity Weight,{frontlineVelocityWeight.ToString("F2", inv)}");
        csv.AppendLine($"Simulation Time Scale,{simulationTimeScale.ToString("F2", inv)}");
        csv.AppendLine();
        csv.AppendLine("=== MODEL ELASTICITIES ===");
        csv.AppendLine("Parameter,Value");
        csv.AppendLine($"Scale Constant,{elasticities.scaleConstant.ToString("F6", inv)}");
        csv.AppendLine($"e_Health,{elasticities.e_Health.ToString("F6", inv)}");
        csv.AppendLine($"e_DPS,{elasticities.e_DPS.ToString("F6", inv)}");
        csv.AppendLine($"e_Range,{elasticities.e_Range.ToString("F6", inv)}");
        csv.AppendLine($"e_MoveSpeed,{elasticities.e_MoveSpeed.ToString("F6", inv)}");
        csv.AppendLine($"e_KBD,{elasticities.e_KBD.ToString("F6", inv)}");
        csv.AppendLine($"e_KBH,{elasticities.e_KBH.ToString("F6", inv)}");
        csv.AppendLine($"Synthetic Training R-Squared,{elasticities.rSquared.ToString("F6", inv)}");
        csv.AppendLine($"Held-Out Real Roster R-Squared (Anchor Excluded),{validationRSquared.ToString("F6", inv)}");
        csv.AppendLine();
        csv.AppendLine("=== UNIT RESULTS ===");
        csv.AppendLine("Clan,Unit,IsSynthetic,SpawnCost,HP,Attack,CycleSeconds,DPS,StoppingRange,MoveSpeed,KBD,KBDPS,KBH,MaxTargets,UtilityPower,AverageTerritoryShiftM,TerminalTerritoryShiftM,AverageCandidateAdvanceM,AverageOpponentAdvanceM,SurvivalSeconds,OpponentSurvivalSeconds,DamageDealt,DamageReceived,CandidateDeathRate,OpponentDeathRate,EmpiricalTerritoryPower,FormulaPredictedPower,FormulaError,ValueDiscrepancy,NormalizedTerminalTerritoryScore");
        foreach (TrialResult r in results)
        {
            csv.AppendLine($"\"{r.clanName}\",\"{r.unitName}\",{r.isSynthetic},{r.spawnCost},{r.rawHP.ToString("F2",inv)},{r.rawAtk.ToString("F2",inv)},{r.rawCycleSec.ToString("F3",inv)},{r.rawDPS.ToString("F2",inv)},{r.rawRange.ToString("F2",inv)},{r.rawMove.ToString("F2",inv)},{r.rawKBD.ToString("F2",inv)},{r.rawKBDPS.ToString("F2",inv)},{r.rawKBH.ToString("F2",inv)},{r.maxTargets},{r.utilityPower.ToString("F2",inv)},{r.averageTerritoryShiftMeters.ToString("F3",inv)},{r.finalTerritoryShiftMeters.ToString("F3",inv)},{r.averageCandidateAdvanceMeters.ToString("F3",inv)},{r.averageOpponentAdvanceMeters.ToString("F3",inv)},{r.timeSurvived.ToString("F2",inv)},{r.averageOpponentSurvivalSeconds.ToString("F2",inv)},{r.averageDamageDealt.ToString("F2",inv)},{r.averageDamageReceived.ToString("F2",inv)},{r.candidateDeathRate.ToString("F3",inv)},{r.opponentDeathRate.ToString("F3",inv)},{r.scaledCalculatedPower.ToString("F2",inv)},{r.formulaPredictedPower.ToString("F2",inv)},{(r.formulaPredictedPower-r.scaledCalculatedPower).ToString("F2",inv)},{(r.scaledCalculatedPower-r.spawnCost).ToString("F2",inv)},{r.compositePowerScore.ToString("F3",inv)}");
        }

        string safeFileName = string.IsNullOrWhiteSpace(diagnosticCsvFileName) ? "BalanceSimulationDiagnostics.csv" : diagnosticCsvFileName;
        if (!safeFileName.EndsWith(".csv")) safeFileName += ".csv";
        string fullPath = Path.Combine(Application.dataPath, safeFileName);
        File.WriteAllText(fullPath, csv.ToString(), Encoding.UTF8);
        Debug.Log($"<b>[BalanceSim] Exported territory-push diagnostic CSV to:</b>\n{fullPath}");
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    string FindClanNameForUnit(ScriptableStats unit)
    {
        if (unit == null) return "Unassigned";
        if (unit == anchorUnit) return "AnchorBaseline";
        if (all_clan_stats != null)
        {
            foreach (var clan in all_clan_stats)
            {
                if (clan == null || clan.all_stats_scripts == null) continue;
                foreach (var cd in clan.all_stats_scripts)
                {
                    if (cd != null && cd.scriptableStats == unit)
                        return string.IsNullOrEmpty(clan.ClanTheme) ? clan.name : clan.ClanTheme;
                }
            }
        }
        return "ExtraUnits";
    }

    public static float EvaluateEstimatedPower(ScriptableStats unit, ScriptableStats anchor, CobbDouglasElasticities e, float anchorCost)
    {
        if (unit == null || anchor == null) return 0f;

        var uProf = PowerMath.GetProfile(unit);
        var aProf = PowerMath.GetProfile(anchor);

        float uFreq = 1f / Mathf.Max(GetEffectiveCombatCycleSeconds(unit), 0.05f);
        float aFreq = 1f / Mathf.Max(GetEffectiveCombatCycleSeconds(anchor), 0.05f);

        float uDPS = Mathf.Max(uProf.Atk * uFreq, 0.01f);
        float aDPS = Mathf.Max(aProf.Atk * aFreq, 0.01f);

        float hpRatio    = Mathf.Max(uProf.HP / Mathf.Max(aProf.HP, 1f), 0.05f);
        float dpsRatio   = Mathf.Max(uDPS / aDPS, 0.05f);
        float rangeRatio = Mathf.Max(GetTrueStoppingRange(unit) / Mathf.Max(GetTrueStoppingRange(anchor), 0.1f), 0.05f);
        float moveRatio  = Mathf.Max(uProf.Move / Mathf.Max(aProf.Move, 0.1f), 0.05f);
        float kbdRatio   = ComputeBufferedKbdRatio(uProf.KBD, uProf.KBD * uFreq, aProf.KBD, aProf.KBD * aFreq);
        float kbhRatio   = Mathf.Max(uProf.KBH / Mathf.Max(aProf.KBH, 1f), 0.05f);

        float scale = e.scaleConstant > 0.001f ? e.scaleConstant : 1.0f;
        float eHP   = e.e_Health > 0f ? e.e_Health : 0.8f;
        float eDPS  = e.e_DPS > 0f ? e.e_DPS : 1.0f;
        float eRng  = e.e_Range;
        float eMove = e.e_MoveSpeed;
        float eKBD  = e.e_KBD;
        float eKBH  = e.e_KBH;

        float powerRatio = scale
                         * Mathf.Pow(hpRatio, eHP)
                         * Mathf.Pow(dpsRatio, eDPS)
                         * Mathf.Pow(rangeRatio, eRng)
                         * Mathf.Pow(moveRatio, eMove)
                         * Mathf.Pow(kbdRatio, eKBD)
                         * Mathf.Pow(kbhRatio, eKBH);

        float utility = PowerMath.GetUtilityPower(unit);
        return (powerRatio * anchorCost) + utility;
    }

    [ContextMenu("Recalculate & Save All ScriptableStats Assets")]
    public void UpdateAllScriptableStatsAndClans()
    {
        AutoWireReferences();
        float anchorCostScale = GetEffectiveAnchorCostScale();

        Dictionary<ScriptableStats, TrialResult> simLookup = new Dictionary<ScriptableStats, TrialResult>();
        foreach (var r in results)
        {
            if (r.statsAsset != null && !r.isSynthetic)
                simLookup[r.statsAsset] = r;
        }

        List<ScriptableStats> allUnits = GatherAllRosterUnits();
        if (anchorUnit != null && !allUnits.Contains(anchorUnit))
            allUnits.Add(anchorUnit);

        int updatedCount = 0;

        foreach (ScriptableStats s in allUnits)
        {
            if (s == null) continue;

            if (simLookup.TryGetValue(s, out TrialResult simResult))
            {
                s._CalculatedPower = simResult.formulaPredictedPower;
            }
            else
            {
                s._CalculatedPower = EvaluateEstimatedPower(s, anchorUnit, elasticities, anchorCostScale);
            }

            s._ValueDiscrepancy = s._CalculatedPower - s._spawnCost;
            s.Level_Total = s._CalculatedPower;
            s.Level_Discrepancy = s._ValueDiscrepancy;

#if UNITY_EDITOR
            EditorUtility.SetDirty(s);
#endif
            updatedCount++;
        }

        if (all_clan_stats != null)
        {
            foreach (ClanStats clan in all_clan_stats)
            {
                if (clan == null || clan.all_stats_scripts == null) continue;

                float tLevel = 0f, tMove = 0f, tKBD = 0f, tAtk = 0f, tCooldown = 0f, tHP = 0f, tKBH = 0f, tRng = 0f;
                int validCount = 0;
                clan.UnitValueDiscrepancies = new float[clan.all_stats_scripts.Length];

                for (int i = 0; i < clan.all_stats_scripts.Length; i++)
                {
                    var cd = clan.all_stats_scripts[i];
                    if (cd == null || cd.scriptableStats == null) continue;

                    ScriptableStats s = cd.scriptableStats;
                    validCount++;
                    clan.UnitValueDiscrepancies[i] = s._ValueDiscrepancy;
                    tLevel += s._CalculatedPower;

                    var prof = PowerMath.GetProfile(s);
                    tMove     += prof.Move;
                    tKBD      += prof.KBD;
                    tAtk      += prof.Atk;
                    tCooldown += GetEffectiveCombatCycleSeconds(s);
                    tHP       += prof.HP;
                    tKBH      += prof.KBH;
                    tRng      += GetTrueStoppingRange(s);
                }

                int div = Mathf.Max(1, validCount);
                clan.TotalLevel  = tLevel;
                clan.AvgMove     = tMove / div;
                clan.AvgKB_Dmg   = tKBD / div;
                clan.AvgAtk_Dmg  = tAtk / div;
                clan.AvgCooldown = tCooldown / div;
                clan.AvgHP       = tHP / div;
                clan.AvgKB_HP    = tKBH / div;
                clan.AvgRange    = tRng / div;

#if UNITY_EDITOR
                EditorUtility.SetDirty(clan);
#endif
            }
        }

#if UNITY_EDITOR
        AssetDatabase.SaveAssets();
#endif
        Debug.Log($"<b>[BalanceSim] Updated & Saved {updatedCount} ScriptableStats using Cobb-Douglas Elasticities!</b>");
    }

    void SolveCobbDouglasRegression()
    {
        List<TrialResult> trainingResults = new List<TrialResult>();
        foreach (TrialResult result in results)
        {
            if (result.statsAsset != anchorUnit)
                trainingResults.Add(result);
        }

        int n = trainingResults.Count;
        const int p = 7;
        if (n < 6) return;

        double[,] X = new double[n, p];
        double[] Y = new double[n];

        for (int i = 0; i < n; i++)
        {
            TrialResult r = trainingResults[i];
            X[i, 0] = 1.0;
            X[i, 1] = System.Math.Log(System.Math.Max(r.hpRatio, 0.05));
            X[i, 2] = System.Math.Log(System.Math.Max(r.dpsRatio, 0.05));
            X[i, 3] = System.Math.Log(System.Math.Max(r.rangeRatio, 0.05));
            X[i, 4] = System.Math.Log(System.Math.Max(r.moveRatio, 0.05));
            X[i, 5] = System.Math.Log(System.Math.Max(r.kbdRatio, 0.25));
            X[i, 6] = System.Math.Log(System.Math.Max(r.kbhRatio, 0.05));

            Y[i] = System.Math.Log(System.Math.Max(r.compositePowerScore, 0.02));
        }

        double[,] XtX = new double[p, p];
        double[] XtY = new double[p];
        for (int j = 0; j < p; j++)
        {
            for (int k = 0; k < p; k++)
            {
                double sum = 0;
                for (int i = 0; i < n; i++) sum += X[i, j] * X[i, k];
                if (j == k && j > 0) sum += 0.15;
                XtX[j, k] = sum;
            }
            for (int i = 0; i < n; i++) XtY[j] += X[i, j] * Y[i];
        }

        double[] beta = GaussianElimination(XtX, XtY, p);
        float solvedScale = Mathf.Clamp(Mathf.Exp((float)beta[0]), 0.35f, 3.0f);
        float eHP    = Mathf.Clamp((float)beta[1], 0.10f, 2.5f);
        float eDPS   = Mathf.Clamp((float)beta[2], 0.10f, 2.5f);
        float eRange = Mathf.Clamp((float)beta[3], 0.05f, 2.5f);
        float eMove  = Mathf.Clamp((float)beta[4], 0.10f, 1.5f);
        float eKBD   = Mathf.Clamp((float)beta[5], 0.05f, 2.0f);
        float eKBH   = Mathf.Clamp((float)beta[6], 0.05f, 1.5f);

        double meanY = 0;
        for (int i = 0; i < n; i++) meanY += Y[i];
        meanY /= n;
        double ssTot = 0, ssRes = 0;
        for (int i = 0; i < n; i++)
        {
            double prediction = System.Math.Log(solvedScale)
                              + X[i, 1] * eHP
                              + X[i, 2] * eDPS
                              + X[i, 3] * eRange
                              + X[i, 4] * eMove
                              + X[i, 5] * eKBD
                              + X[i, 6] * eKBH;
            ssRes += (Y[i] - prediction) * (Y[i] - prediction);
            ssTot += (Y[i] - meanY) * (Y[i] - meanY);
        }
        float r2 = ssTot > 1e-6 ? (float)System.Math.Max(0.0, 1.0 - ssRes / ssTot) : 1f;

        elasticities = new CobbDouglasElasticities
        {
            scaleConstant = solvedScale,
            e_Health      = eHP,
            e_DPS         = eDPS,
            e_Range       = eRange,
            e_MoveSpeed   = eMove,
            e_KBD         = eKBD,
            e_KBH         = eKBH,
            rSquared      = r2
        };
#if UNITY_EDITOR
        if (balanceConfig != null)
        {
            balanceConfig.anchorUnit = anchorUnit;
            balanceConfig.anchorCost = GetEffectiveAnchorCostScale();
            balanceConfig.elasticities = elasticities;
            EditorUtility.SetDirty(balanceConfig);
            AssetDatabase.SaveAssets();
        }
#endif
    }

    double[] GaussianElimination(double[,] A, double[] b, int n)
    {
        double[] x = new double[n];
        for (int i = 0; i < n; i++)
        {
            int maxRow = i;
            for (int k = i + 1; k < n; k++)
                if (System.Math.Abs(A[k, i]) > System.Math.Abs(A[maxRow, i])) maxRow = k;

            for (int k = i; k < n; k++) { double tmp = A[maxRow, k]; A[maxRow, k] = A[i, k]; A[i, k] = tmp; }
            double tmpB = b[maxRow]; b[maxRow] = b[i]; b[i] = tmpB;

            if (System.Math.Abs(A[i, i]) < 1e-12) continue;

            for (int k = i + 1; k < n; k++)
            {
                double factor = A[k, i] / A[i, i];
                b[k] -= factor * b[i];
                for (int j = i; j < n; j++) A[k, j] -= factor * A[i, j];
            }
        }

        for (int i = n - 1; i >= 0; i--)
        {
            if (System.Math.Abs(A[i, i]) < 1e-12) { x[i] = 0; continue; }
            double sum = b[i];
            for (int j = i + 1; j < n; j++) sum -= A[i, j] * x[j];
            x[i] = sum / A[i, i];
        }
        return x;
    }

    List<ScriptableStats> GatherAllRosterUnits()
    {
        List<ScriptableStats> list = new List<ScriptableStats>();
        if (all_clan_stats != null)
        {
            foreach (var clan in all_clan_stats)
            {
                if (clan == null || clan.all_stats_scripts == null) continue;
                foreach (var cd in clan.all_stats_scripts)
                {
                    if (cd != null && cd.scriptableStats != null && !list.Contains(cd.scriptableStats))
                        list.Add(cd.scriptableStats);
                }
            }
        }

        if (extraUnitsToTest != null)
        {
            foreach (var u in extraUnitsToTest)
            {
                if (u != null && !list.Contains(u)) list.Add(u);
            }
        }
        return list;
    }

    float GetEffectiveAnchorCostScale()
    {
        if (costPerAnchorEquivalent > 0f) return costPerAnchorEquivalent;
        if (anchorUnit != null && anchorUnit._spawnCost > 0) return anchorUnit._spawnCost;
        return 1f;
    }

    float GetOutlierResistantAverage(List<float> values)
    {
        if (values == null || values.Count == 0) return 0f;
        if (values.Count == 1) return values[0];

        values.Sort();

        if (values.Count == 2) return (values[0] + values[1]) * 0.5f;

        if (values.Count <= 4)
        {
            int mid = values.Count / 2;
            return (values.Count % 2 != 0) ? values[mid] : (values[mid - 1] + values[mid]) * 0.5f;
        }

        float sum = 0f;
        for (int i = 1; i < values.Count - 1; i++) sum += values[i];
        return sum / (values.Count - 2);
    }

    IEnumerator RunSingleTrial(ScriptableStats testUnit, bool reverseSides, System.Action<TrialSample> onComplete)
    {
        CleanupTrialObjects();
        yield return new WaitForFixedUpdate();

        SpawnObjects subjectSpawner = reverseSides ? enemySpawner : alliedSpawner;
        SpawnObjects opponentSpawner = reverseSides ? alliedSpawner : enemySpawner;
        float forwardSign = Mathf.Sign(opponentSpawner.transform.position.x - subjectSpawner.transform.position.x);
        if (forwardSign == 0f) forwardSign = reverseSides ? -1f : 1f;

        float maximumReach = Mathf.Max(GetMaximumHostileActionReach(testUnit), GetMaximumHostileActionReach(anchorUnit));
        float minimumSeparation = Mathf.Max(2f, maximumReach + Mathf.Max(0.5f, spawnDistanceMargin));

        float CalculateNextWaveDistance()
        {
            bool useFar = RandomRange(0f, 1f) < Mathf.Clamp01(farSpawnTrialChance);
            float jitter = RandomRange(-anchorSpawnDistanceJitterMeters, anchorSpawnDistanceJitterMeters);
            float baseDist = Mathf.Max(minimumSeparation, waveReinforcementDistanceMeters + jitter);
            if (useFar) baseDist += Mathf.Max(0f, farSpawnExtraMeters);
            return baseDist;
        }

        float initialSeparation = CalculateNextWaveDistance();
        float midpointX = (alliedSpawner.transform.position.x + enemySpawner.transform.position.x) * 0.5f;
        Vector3 subjectStart = new Vector3(
            midpointX - forwardSign * initialSeparation * 0.5f,
            subjectSpawner.transform.position.y,
            subjectSpawner.transform.position.z);

        GameObject subjectGO = subjectSpawner.SpawnCPU(testUnit);
        if (subjectGO == null)
        {
            CleanupTrialObjects();
            onComplete?.Invoke(default);
            yield break;
        }

        _activeSpawnedObjects.Add(subjectGO);
        subjectGO.transform.position = subjectStart;
        Physics2D.SyncTransforms();

        Stats subjectStats = subjectGO.GetComponent<Stats>();
        if (subjectStats == null)
        {
            CleanupTrialObjects();
            onComplete?.Invoke(default);
            yield break;
        }

        Vector3 subjectInitial = subjectStart;
        float elapsed = 0f;
        float territoryIntegral = 0f;
        float candidateAdvance = 0f;
        float opponentAdvance = 0f;
        float effectiveDamageDealt = 0f;
        float damageReceived = 0f;
        float subjectMaxHP = Mathf.Max(1f, subjectStats._MaxHealth);
        float previousSubjectHP = Mathf.Max(0f, subjectStats._CurrentHealth);

        float spawnDelay = RandomRange(0f, Mathf.Max(0f, anchorSpawnDelayJitterSeconds));
        while (elapsed < spawnDelay && elapsed < maxTrialDuration)
        {
            yield return new WaitForFixedUpdate();
            float dt = Time.fixedDeltaTime;
            elapsed += dt;
            if (subjectGO == null || subjectStats == null || subjectStats._IsDead) break;
            candidateAdvance = (subjectGO.transform.position.x - subjectInitial.x) * forwardSign;
            territoryIntegral += candidateAdvance * dt;
        }

        int anchorsSpawned = 0;
        int anchorsDefeated = 0;
        GameObject currentOpponentGO = null;
        Stats currentOpponentStats = null;
        Vector3 currentOpponentSpawnPos = subjectStart;
        float previousOpponentHP = 0f;
        float firstOpponentSurvivalSec = 0f;
        float currentWaveSpawnTime = elapsed;

        void SpawnNextAnchorWave(float distanceAhead)
        {
            if (subjectGO == null) return;
            currentOpponentSpawnPos = new Vector3(
                subjectGO.transform.position.x + forwardSign * distanceAhead,
                opponentSpawner.transform.position.y,
                opponentSpawner.transform.position.z);

            currentOpponentGO = opponentSpawner.SpawnCPU(anchorUnit);
            if (currentOpponentGO == null) return;

            _activeSpawnedObjects.Add(currentOpponentGO);
            currentOpponentGO.transform.position = currentOpponentSpawnPos;
            Physics2D.SyncTransforms();

            currentOpponentStats = currentOpponentGO.GetComponent<Stats>();
            if (currentOpponentStats != null)
            {
                // Progressive wave escalation (+18% per wave) + random stat jitter so no unit can infinite-stunlock
                // forever and every unit is tested until defeat!
                float escalation = 1f + (anchorsSpawned * Mathf.Max(0f, waveEscalationPerWave));
                float hpMult  = escalation * (1f + RandomRange(-anchorStatJitterFraction, anchorStatJitterFraction));
                float kbhMult = escalation * (1f + RandomRange(-anchorStatJitterFraction, anchorStatJitterFraction));
                float kbdMult = escalation * (1f + RandomRange(-anchorStatJitterFraction, anchorStatJitterFraction));
                float atkMult = escalation;

                float jitteredHP = Mathf.Max(5f, anchorUnit._MaxHealth * hpMult);
                currentOpponentStats._MaxHealth = Mathf.RoundToInt(jitteredHP);
                currentOpponentStats._CurrentHealth = jitteredHP;
                currentOpponentStats._AttackDamage = Mathf.Max(1, Mathf.RoundToInt(anchorUnit._AttackDamage * atkMult));
                currentOpponentStats._KnockBackMaxHealth = Mathf.Max(2f, anchorUnit._KnockBackMaxHealth * kbhMult);
                currentOpponentStats._KnockBackHealth = currentOpponentStats._KnockBackMaxHealth;
                currentOpponentStats._KnockBackDamage = Mathf.Max(1f, anchorUnit._KnockBackDamage * kbdMult);
                previousOpponentHP = jitteredHP;
            }
            anchorsSpawned++;
            currentWaveSpawnTime = elapsed;
        }

        SpawnNextAnchorWave(initialSeparation);

        float candidateSingleHitDamage = Mathf.Max(0f, PowerMath.GetProfile(testUnit).Atk);

        while (elapsed < maxTrialDuration)
        {
            yield return new WaitForFixedUpdate();
            float dt = Time.fixedDeltaTime;
            elapsed += dt;

            if (subjectStats != null)
            {
                float hp = Mathf.Max(0f, subjectStats._CurrentHealth);
                if (hp < previousSubjectHP) damageReceived += previousSubjectHP - hp;
                previousSubjectHP = hp;
            }

            if (currentOpponentStats != null)
            {
                float hp = Mathf.Max(0f, currentOpponentStats._CurrentHealth);
                if (hp < previousOpponentHP)
                {
                    float actualDrop = previousOpponentHP - hp;
                    if (hp <= 0f && candidateSingleHitDamage > actualDrop)
                    {
                        float overkill = candidateSingleHitDamage - actualDrop;
                        effectiveDamageDealt += actualDrop + (overkill * 0.5f);
                    }
                    else
                    {
                        effectiveDamageDealt += actualDrop;
                    }
                    previousOpponentHP = hp;
                }
            }

            if (subjectGO != null)
                candidateAdvance = (subjectGO.transform.position.x - subjectInitial.x) * forwardSign;

            if (currentOpponentGO != null)
                opponentAdvance = (currentOpponentGO.transform.position.x - currentOpponentSpawnPos.x) * forwardSign;

            territoryIntegral += candidateAdvance * dt;

            bool subjectDead = subjectGO == null || subjectStats == null || subjectStats._IsDead;
            if (subjectDead) break;

            bool opponentDead = currentOpponentGO == null || currentOpponentStats == null || currentOpponentStats._IsDead;
            if (opponentDead)
            {
                anchorsDefeated++;
                if (anchorsDefeated == 1)
                    firstOpponentSurvivalSec = Mathf.Max(0f, elapsed - currentWaveSpawnTime);

                if (anchorsSpawned >= maxAnchorsPerTrial)
                    break;

                SpawnNextAnchorWave(CalculateNextWaveDistance());
            }
        }

        bool candidateDiedFinal = subjectGO == null || subjectStats == null || subjectStats._IsDead;
        float remainingHpRatio = (!candidateDiedFinal && subjectStats != null)
            ? Mathf.Clamp01(subjectStats._CurrentHealth / subjectMaxHP)
            : 0f;

        if (firstOpponentSurvivalSec <= 0f)
            firstOpponentSurvivalSec = elapsed;

        TrialSample sample = new TrialSample
        {
            averageTerritoryShiftMeters = elapsed > 0f ? territoryIntegral / elapsed : candidateAdvance,
            finalTerritoryShiftMeters = candidateAdvance,
            candidateAdvanceMeters = candidateAdvance,
            opponentAdvanceMeters = opponentAdvance,
            candidateSurvivalSeconds = elapsed,
            opponentSurvivalSeconds = firstOpponentSurvivalSec,
            effectiveDamageDealt = effectiveDamageDealt,
            damageReceived = damageReceived,
            remainingCandidateHpRatio = remainingHpRatio,
            anchorsDefeatedCount = anchorsDefeated,
            candidateDied = candidateDiedFinal,
            opponentDied = anchorsDefeated > 0
        };

        CleanupTrialObjects();
        onComplete?.Invoke(sample);
    }

    float GetMaximumHostileActionReach(ScriptableStats stats)
    {
        if (stats == null) return 0.5f;
        float maxReach = 0f;
        if (stats.combatActions != null)
        {
            foreach (CombatAction action in stats.combatActions)
            {
                if (action == null || action.targetFriendly || action.maxTargets <= 0) continue;
                float radius = Mathf.Max(0f, stats._HorizontalRange * action.rangePercent);
                float forwardReach = action.extendsForward ? radius * 1.5f : radius;
                if (forwardReach > maxReach) maxReach = forwardReach;
            }
        }
        return maxReach > 0f ? maxReach : Mathf.Max(0.5f, GetTrueStoppingRange(stats));
    }

    void CleanupTrialObjects()
    {
        for (int i = _activeSpawnedObjects.Count - 1; i >= 0; i--)
        {
            if (_activeSpawnedObjects[i] != null) Destroy(_activeSpawnedObjects[i]);
        }
        _activeSpawnedObjects.Clear();
    }

    public static float GetEffectiveCombatCycleSeconds(ScriptableStats s)
    {
        var primary = PowerMath.GetPrimaryOffensiveAction(s.combatActions);
        float castMult = primary != null ? primary.castCooldown : 1f;
        float startupSec = s._AnimationStartupTime / 1000f;
        float recoverySec = s._AnimationRecoveryTime / 1000f;
        float endlagSec = s._ExtraEndlag;
        float moveCooldownSec = s._ActionCooldown * castMult * 0.5f;

        // Floor attack cycle at 0.45s so low _ActionCooldown values on synthetic clones don't predict
        // faster-than-animation attack rates!
        return Mathf.Max(startupSec + recoverySec + endlagSec + moveCooldownSec, 0.45f);
    }

    public static float GetTrueAttackCycleSeconds(ScriptableStats s) => GetEffectiveCombatCycleSeconds(s);

    public static float GetTrueStoppingRange(ScriptableStats s)
    {
        if (s == null) return 0f;
        if (s.combatActions == null || s.combatActions.Count == 0) return Mathf.Max(0f, s._HorizontalRange);

        bool anyDefined = false;
        foreach (CombatAction action in s.combatActions)
            if (action != null && action.definesStoppingRange) { anyDefined = true; break; }

        float maxReach = 0f;
        CombatAction highestPriority = null;
        foreach (CombatAction action in s.combatActions)
        {
            if (action == null) continue;
            if (highestPriority == null || action.priority > highestPriority.priority) highestPriority = action;
            if (anyDefined && !action.definesStoppingRange) continue;
            float radius = Mathf.Max(0f, s._HorizontalRange * action.rangePercent);
            float reach = action.extendsForward ? radius * 1.5f : radius;
            if (reach > maxReach) maxReach = reach;
        }

        if (!anyDefined && highestPriority != null)
        {
            float radius = Mathf.Max(0f, s._HorizontalRange * highestPriority.rangePercent);
            maxReach = highestPriority.extendsForward ? radius * 1.5f : radius;
        }
        return maxReach > 0f ? maxReach : Mathf.Max(0f, s._HorizontalRange);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BalanceSimulationRunner))]
public class BalanceSimulationRunnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BalanceSimulationRunner runner = (BalanceSimulationRunner)target;
        GUILayout.Space(10);

        if (GUILayout.Button("1. Auto-Grab References from Project", GUILayout.Height(24)))
        {
            Undo.RecordObject(runner, "Auto-Grab References");
            runner.AutoWireReferences();
            EditorUtility.SetDirty(runner);
        }

        GUILayout.Space(4);
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("2. ▶ Run Full Roster Simulation (Play Mode)", GUILayout.Height(32)))
        {
            runner.StartSimulationSuite();
        }

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("■ Stop Simulation", GUILayout.Height(22)))
        {
            runner.StopSimulationSuite();
        }

        GUI.backgroundColor = Color.white;
        GUILayout.Space(4);
        if (GUILayout.Button("3. Recalculate & Save All ScriptableStats Now", GUILayout.Height(26)))
        {
            runner.UpdateAllScriptableStatsAndClans();
        }

        GUILayout.Space(4);
        GUI.backgroundColor = new Color(0.2f, 0.7f, 1.0f);
        if (GUILayout.Button("4. 📊 Export Full Diagnostic CSV (Inputs & Outputs)", GUILayout.Height(32)))
        {
            runner.ExportDiagnosticCSV();
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif