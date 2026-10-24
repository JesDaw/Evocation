using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BalanceSimulationRunner : MonoBehaviour
{
    [Header("1. Spawner References")]
    [Tooltip("The Allied SpawnObjects component (spawns test characters on player/allied side).")]
    public SpawnObjects alliedSpawner;

    [Tooltip("The Enemy SpawnObjects component (spawns Anchor units on enemy side).")]
    public SpawnObjects enemySpawner;

    [Header("2. Game Roster (Clans & Characters)")]
    [Tooltip("The fixed baseline unit that every character in the game is tested against.")]
    public ScriptableStats anchorUnit;

    [Tooltip("All ClanStats assets in the game. Every character inside these clans will be simulated.")]
    public ClanStats[] all_clan_stats;

    [Tooltip("Optional individual characters to test and calibrate in addition to the clans above.")]
    public List<ScriptableStats> extraUnitsToTest = new List<ScriptableStats>();

    [Header("3. Cost & Power Scaling")]
    [Tooltip("Scales simulated Anchor-Equivalents into Spawn Cost units. If 0, uses AnchorUnit._spawnCost.")]
    public float costPerAnchorEquivalent = 0f;

    [Tooltip("If true, automatically writes the empirical simulation score + level breakdown into each character's ScriptableStats asset when finished.")]
    public bool autoUpdateScriptableStats = true;

    [Header("4. Simulation Mode & Outlier Protection")]
    [Tooltip("How many times to test each character. 3+ uses Median/Trimmed-Mean to throw out physics outliers.")]
    [Range(1, 9)] public int repeatsPerUnit = 3;

    [Tooltip("FALSE = Only tests your real characters. TRUE = Also runs randomized stat clones of the Anchor for extra regression stability.")]
    public bool includeSyntheticTrials = false;

    [Tooltip("Number of randomized Anchor clones to test if synthetic trials are enabled.")]
    public int syntheticTrialCount = 20;

    [Header("5. Battle Rules & Speed")]
    [Tooltip("Fast-forwards Unity physics during the simulation (6–10 is the sweet spot).")]
    [Range(1f, 20f)] public float simulationTimeScale = 8f;

    [Tooltip("0 = Sequential 1v1 Gauntlet (a new Anchor spawns when the current one dies). > 0 = Continuous Swarm.")]
    public float anchorStreamInterval = 0f;

    [Tooltip("Maximum game-seconds a single battle can last before timing out.")]
    public float maxTrialDuration = 45f;

    [Tooltip("Maximum number of Anchors a single unit can kill before ending the trial as a decisive win.")]
    public int maxAnchorsPerTrial = 15;

    [Header("6. Live Progress (Read-Only)")]
    public bool isRunning = false;
    public string currentStatus = "Idle";
    public List<TrialResult> results = new List<TrialResult>();

    [Header("7. Solved Stat Weights (Output)")]
    public SolvedWeights solvedWeights;

    private List<GameObject> _activeSpawnedObjects = new List<GameObject>();

    [System.Serializable]
    public struct TrialResult
    {
        public string unitName;
        public ScriptableStats statsAsset;
        public float hp, atk, move, range, freq, kbd, kbh;
        public float anchorsKilledEquivalent;
        public float timeSurvived;
        public float normalizedLanePush;
        public float compositePowerScore;
        public float scaledCalculatedPower;
    }

    [System.Serializable]
    public struct SolvedWeights
    {
        public float w_MaxHealth;
        public float w_AttackDamage;
        public float w_AttackFrequency;
        public float w_MoveSpeed;
        public float w_HorizontalRange;
        public float w_KnockBackDamage;
        public float w_KnockBackHealth;
        public float rSquared;
    }

    [ContextMenu("Auto-Grab References from Scene")]
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
            Debug.Log($"[BalanceSim] Auto-found {all_clan_stats.Length} ClanStats assets in project.");
#endif
        }
    }

    public void StartSimulationSuite()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[BalanceSim] Enter PLAY MODE first to run the live simulation!");
            return;
        }
        if (isRunning) return;
        StartCoroutine(RunSuiteCoroutine());
    }

    public void StopSimulationSuite()
    {
        if (!isRunning) return;
        StopAllCoroutines();
        CleanupTrialObjects();
        Time.timeScale = 1f;
        isRunning = false;
        currentStatus = "Stopped by user.";
    }

    private IEnumerator RunSuiteCoroutine()
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

        alliedSpawner.spawningEnabled = true;
        enemySpawner.spawningEnabled = true;

        List<ScriptableStats> testPool = GatherAllRosterUnits();
        List<ScriptableStats> tempClonesToDestroy = new List<ScriptableStats>();

        if (includeSyntheticTrials)
        {
            for (int i = 0; i < syntheticTrialCount; i++)
            {
                ScriptableStats clone = Instantiate(anchorUnit);
                clone.name = $"SynthTrial_{i + 1}";
                clone._MaxHealth          = Mathf.Max(1, Mathf.RoundToInt(anchorUnit._MaxHealth * Random.Range(0.5f, 3.5f)));
                clone._AttackDamage       = Mathf.Max(1, Mathf.RoundToInt(anchorUnit._AttackDamage * Random.Range(0.5f, 3.5f)));
                clone._MoveSpeed          = anchorUnit._MoveSpeed * Random.Range(0.5f, 2.5f);
                clone._HorizontalRange    = anchorUnit._HorizontalRange * Random.Range(0.5f, 3.0f);
                clone._ActionCooldown     = Mathf.Max(0.2f, anchorUnit._ActionCooldown * Random.Range(0.4f, 2.0f));
                clone._KnockBackDamage    = anchorUnit._KnockBackDamage * Random.Range(0.2f, 3.5f);
                clone._KnockBackMaxHealth = Mathf.Max(1f, anchorUnit._KnockBackMaxHealth * Random.Range(0.5f, 3.5f));

                testPool.Add(clone);
                tempClonesToDestroy.Add(clone);
            }
        }

        if (testPool.Count == 0)
        {
            Debug.LogError("[BalanceSim] No characters found to test!");
            isRunning = false;
            yield break;
        }

        float originalTimeScale = Time.timeScale;
        Time.timeScale = simulationTimeScale;

        float anchorCostScale = GetEffectiveAnchorCostScale();

        for (int i = 0; i < testPool.Count; i++)
        {
            ScriptableStats candidate = testPool[i];
            List<float> runAnchors = new List<float>();
            List<float> runTimes = new List<float>();
            List<float> runPushes = new List<float>();

            int repeats = Mathf.Max(1, repeatsPerUnit);
            for (int r = 0; r < repeats; r++)
            {
                currentStatus = $"Testing [{i + 1}/{testPool.Count}] {candidate.name} (Run {r + 1}/{repeats})";
                yield return StartCoroutine(RunSingleTrial(candidate, (anchorsKilled, timeSurvived, lanePush) =>
                {
                    runAnchors.Add(anchorsKilled);
                    runTimes.Add(timeSurvived);
                    runPushes.Add(lanePush);
                }));
            }

            float avgAnchors = GetOutlierResistantAverage(runAnchors);
            float avgTime    = GetOutlierResistantAverage(runTimes);
            float avgPush    = GetOutlierResistantAverage(runPushes);

            var profile = PowerMath.GetProfile(candidate);
            float trueCycleSec = GetTrueAttackCycleSeconds(candidate);
            float freq = 1f / Mathf.Max(trueCycleSec, 0.05f);

            float compositeScore = avgAnchors + (avgPush * 0.5f);
            float utilityBonus = PowerMath.GetUtilityPower(candidate);
            float scaledPower = (compositeScore * anchorCostScale) + utilityBonus;

            bool isTempClone = tempClonesToDestroy.Contains(candidate);

            results.Add(new TrialResult
            {
                unitName = candidate.name,
                statsAsset = isTempClone ? null : candidate,
                hp = profile.HP,
                atk = profile.Atk,
                move = profile.Move,
                range = GetTrueStoppingRange(candidate),
                freq = freq,
                kbd = profile.KBD,
                kbh = profile.KBH,
                anchorsKilledEquivalent = avgAnchors,
                timeSurvived = avgTime,
                normalizedLanePush = avgPush,
                compositePowerScore = compositeScore,
                scaledCalculatedPower = scaledPower
            });
        }

        Time.timeScale = originalTimeScale;
        foreach (var c in tempClonesToDestroy) Destroy(c);

        SolveMultivariableRegression();

        if (autoUpdateScriptableStats)
        {
            UpdateAllScriptableStatsAndClans();
        }

        currentStatus = $"Done! Updated {results.Count} units. R² = {solvedWeights.rSquared:F3}";
        isRunning = false;
    }

    [ContextMenu("Recalculate & Save All ScriptableStats Assets")]
    public void UpdateAllScriptableStatsAndClans()
    {
        AutoWireReferences();
        float anchorCostScale = GetEffectiveAnchorCostScale();

        Dictionary<ScriptableStats, TrialResult> simLookup = new Dictionary<ScriptableStats, TrialResult>();
        foreach (var r in results)
        {
            if (r.statsAsset != null) simLookup[r.statsAsset] = r;
        }

        List<ScriptableStats> allUnits = GatherAllRosterUnits();
        int updatedCount = 0;

        foreach (ScriptableStats s in allUnits)
        {
            if (s == null) continue;

            var profile = PowerMath.GetProfile(s);
            float trueCycleSec = GetTrueAttackCycleSeconds(s);
            float freq = 1f / Mathf.Max(trueCycleSec, 0.05f);
            float trueRange = GetTrueStoppingRange(s);

            float wHP   = Mathf.Max(solvedWeights.w_MaxHealth, 0.001f) * anchorCostScale;
            float wAtk  = Mathf.Max(solvedWeights.w_AttackDamage, 0.001f) * anchorCostScale;
            float wFreq = Mathf.Max(solvedWeights.w_AttackFrequency, 0.001f) * anchorCostScale;
            float wMove = Mathf.Max(solvedWeights.w_MoveSpeed, 0.001f) * anchorCostScale;
            float wRng  = Mathf.Max(solvedWeights.w_HorizontalRange, 0.001f) * anchorCostScale;
            float wKBD  = Mathf.Max(solvedWeights.w_KnockBackDamage, 0.001f) * anchorCostScale;
            float wKBH  = Mathf.Max(solvedWeights.w_KnockBackHealth, 0.001f) * anchorCostScale;

            s.Attack          = (profile.Atk * wAtk) + (profile.KBD * wKBD);
            s.Defense         = (profile.HP * wHP) + (profile.KBH * wKBH);
            s.SpaceControl    = (trueRange * wRng) + (profile.Move * wMove);
            s.AttackFrequency = freq * wFreq;
            s.Level_Total     = s.Attack + s.Defense + s.SpaceControl + s.AttackFrequency;
            s.Level_Discrepancy = s.Level_Total - s._spawnCost;

            float utilityPower = PowerMath.GetUtilityPower(s);
            if (simLookup.TryGetValue(s, out TrialResult simResult))
            {
                s._CalculatedPower = simResult.scaledCalculatedPower;
            }
            else
            {
                s._CalculatedPower = s.Level_Total + utilityPower;
            }

            s._ValueDiscrepancy = s._CalculatedPower - s._spawnCost;

#if UNITY_EDITOR
            EditorUtility.SetDirty(s);
#endif
            updatedCount++;
        }

        // Update all ClanStats summaries
        if (all_clan_stats != null)
        {
            foreach (ClanStats clan in all_clan_stats)
            {
                if (clan == null || clan.all_stats_scripts == null) continue;

                float tAttack = 0f, tDefense = 0f, tSpace = 0f, tFreq = 0f, tLevel = 0f;
                float tMove = 0f, tKBD = 0f, tAtk = 0f, tCooldown = 0f, tHP = 0f, tKBH = 0f, tRng = 0f;
                int validCount = 0;

                clan.UnitValueDiscrepancies = new float[clan.all_stats_scripts.Length];

                for (int i = 0; i < clan.all_stats_scripts.Length; i++)
                {
                    var cd = clan.all_stats_scripts[i];
                    if (cd == null || cd.scriptableStats == null) continue;

                    ScriptableStats s = cd.scriptableStats;
                    validCount++;

                    clan.UnitValueDiscrepancies[i] = s._ValueDiscrepancy;
                    tAttack  += s.Attack;
                    tDefense += s.Defense;
                    tSpace   += s.SpaceControl;
                    tFreq    += s.AttackFrequency;
                    tLevel   += s._CalculatedPower;

                    var prof = PowerMath.GetProfile(s);
                    tMove     += prof.Move;
                    tKBD      += prof.KBD;
                    tAtk      += prof.Atk;
                    tCooldown += GetTrueAttackCycleSeconds(s);
                    tHP       += prof.HP;
                    tKBH      += prof.KBH;
                    tRng      += GetTrueStoppingRange(s);
                }

                int div = Mathf.Max(1, validCount);
                clan.TotalLevel         = tLevel;
                clan.SumAttack          = tAttack;
                clan.SumDefense         = tDefense;
                clan.SumSpaceControl    = tSpace;
                clan.AvgAttackFrequency = tFreq / div;

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
        Debug.Log($"<b>[BalanceSim] Updated & Saved {updatedCount} ScriptableStats assets!</b>");
    }

    private List<ScriptableStats> GatherAllRosterUnits()
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

    private float GetEffectiveAnchorCostScale()
    {
        if (costPerAnchorEquivalent > 0f) return costPerAnchorEquivalent;
        if (anchorUnit != null && anchorUnit._spawnCost > 0) return anchorUnit._spawnCost;
        return 1f;
    }

    private float GetOutlierResistantAverage(List<float> values)
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

    private IEnumerator RunSingleTrial(ScriptableStats testUnit, System.Action<float, float, float> onComplete)
    {
        CleanupTrialObjects();
        yield return new WaitForFixedUpdate();

        GameObject allyGO = alliedSpawner.SpawnCPU(testUnit);
        if (allyGO == null)
        {
            onComplete?.Invoke(0f, 0f, 0f);
            yield break;
        }
        _activeSpawnedObjects.Add(allyGO);
        Stats allyStats = allyGO.GetComponent<Stats>();
        Vector3 allyStartPos = allyGO.transform.position;

        List<Stats> spawnedAnchors = new List<Stats>();
        Dictionary<Stats, float> anchorLastHP = new Dictionary<Stats, float>();

        GameObject firstAnchorGO = enemySpawner.SpawnCPU(anchorUnit);
        Vector3 enemyStartPos = firstAnchorGO != null ? firstAnchorGO.transform.position : enemySpawner.transform.position;
        float totalLaneDist = Mathf.Max(Mathf.Abs(enemyStartPos.x - allyStartPos.x), 1f);

        if (firstAnchorGO != null)
        {
            _activeSpawnedObjects.Add(firstAnchorGO);
            if (firstAnchorGO.TryGetComponent(out Stats firstAnchorStats))
            {
                spawnedAnchors.Add(firstAnchorStats);
                anchorLastHP[firstAnchorStats] = firstAnchorStats._CurrentHealth;
            }
        }

        float totalAnchorDamageTaken = 0f;
        int anchorsSpawnedCount = 1;
        int anchorsKilledCount = 0;
        float elapsed = 0f;
        float streamTimer = 0f;
        float maxNormalizedProgress = 0f;
        float candidateStopRange = GetTrueStoppingRange(testUnit);

        while (elapsed < maxTrialDuration)
        {
            yield return new WaitForFixedUpdate();
            float dt = Time.fixedDeltaTime;
            elapsed += dt;
            streamTimer += dt;

            for (int i = spawnedAnchors.Count - 1; i >= 0; i--)
            {
                Stats a = spawnedAnchors[i];
                if (a == null)
                {
                    spawnedAnchors.RemoveAt(i);
                    continue;
                }

                float lastHP = anchorLastHP.ContainsKey(a) ? anchorLastHP[a] : a._MaxHealth;
                float currentHP = Mathf.Max(0f, a._CurrentHealth);
                if (currentHP < lastHP)
                {
                    totalAnchorDamageTaken += (lastHP - currentHP);
                    anchorLastHP[a] = currentHP;
                }

                if (a._IsDead)
                {
                    anchorsKilledCount++;
                    spawnedAnchors.RemoveAt(i);
                }
            }

            if (allyGO == null || allyStats == null || allyStats._IsDead) break;

            if (anchorsKilledCount >= maxAnchorsPerTrial && spawnedAnchors.Count == 0)
            {
                maxNormalizedProgress = 1f;
                break;
            }

            float distFromStart = Mathf.Abs(allyGO.transform.position.x - allyStartPos.x);
            float effectiveReach = distFromStart + candidateStopRange;
            float normalizedProgress = Mathf.Clamp01(effectiveReach / totalLaneDist);
            if (normalizedProgress > maxNormalizedProgress) maxNormalizedProgress = normalizedProgress;

            bool needNextAnchor = anchorsSpawnedCount < maxAnchorsPerTrial &&
                                  ((anchorStreamInterval <= 0f && spawnedAnchors.Count == 0) ||
                                   (anchorStreamInterval > 0f && streamTimer >= anchorStreamInterval));

            if (needNextAnchor)
            {
                streamTimer = 0f;
                anchorsSpawnedCount++;
                GameObject nextAnchorGO = enemySpawner.SpawnCPU(anchorUnit);
                if (nextAnchorGO != null)
                {
                    _activeSpawnedObjects.Add(nextAnchorGO);
                    if (nextAnchorGO.TryGetComponent(out Stats nextAnchorStats))
                    {
                        spawnedAnchors.Add(nextAnchorStats);
                        anchorLastHP[nextAnchorStats] = nextAnchorStats._CurrentHealth;
                    }
                }
            }
        }

        float anchorsKilledEq = totalAnchorDamageTaken / Mathf.Max(1f, anchorUnit._MaxHealth);
        CleanupTrialObjects();
        onComplete?.Invoke(anchorsKilledEq, elapsed, maxNormalizedProgress);
    }

    private void CleanupTrialObjects()
    {
        for (int i = _activeSpawnedObjects.Count - 1; i >= 0; i--)
        {
            if (_activeSpawnedObjects[i] != null) Destroy(_activeSpawnedObjects[i]);
        }
        _activeSpawnedObjects.Clear();
    }

    public static float GetTrueAttackCycleSeconds(ScriptableStats s)
    {
        var primary = PowerMath.GetPrimaryOffensiveAction(s.combatActions);
        float castMult = primary != null ? primary.castCooldown : 1f;
        float startupSec = s._AnimationStartupTime / 1000f;
        float recoverySec = s._AnimationRecoveryTime / 1000f;
        float endlagSec = s._ExtraEndlag;
        float moveCooldownSec = s._ActionCooldown * castMult;

        return Mathf.Max(startupSec + recoverySec + endlagSec + moveCooldownSec, 0.05f);
    }

    public static float GetTrueStoppingRange(ScriptableStats s)
    {
        var primary = PowerMath.GetPrimaryOffensiveAction(s.combatActions);
        if (primary == null) return s._HorizontalRange;
        float baseReach = s._HorizontalRange * primary.rangePercent;
        return primary.extendsForward ? baseReach * 1.5f : baseReach;
    }

    private void SolveMultivariableRegression()
    {
        int n = results.Count;
        int p = 7;
        if (n == 0) return;

        double[,] X = new double[n, p];
        double[] Y = new double[n];
        double[] statScale = new double[p];

        for (int i = 0; i < n; i++)
        {
            var r = results[i];
            X[i, 0] = r.hp;
            X[i, 1] = r.atk;
            X[i, 2] = r.freq;
            X[i, 3] = r.move;
            X[i, 4] = r.range;
            X[i, 5] = r.kbd;
            X[i, 6] = r.kbh;
            Y[i] = r.compositePowerScore;

            for (int j = 0; j < p; j++) statScale[j] += X[i, j] * X[i, j];
        }

        double[,] XtX = new double[p, p];
        double[] XtY = new double[p];
        double ridgeLambda = (n < p) ? 0.15 : 0.01;

        for (int j = 0; j < p; j++)
        {
            for (int k = 0; k < p; k++)
            {
                double sum = 0;
                for (int i = 0; i < n; i++) sum += X[i, j] * X[i, k];
                if (j == k) sum += System.Math.Max(statScale[j] * ridgeLambda, 1e-3);
                XtX[j, k] = sum;
            }

            double sumY = 0;
            for (int i = 0; i < n; i++) sumY += X[i, j] * Y[i];
            XtY[j] = sumY;
        }

        double[] beta = GaussianElimination(XtX, XtY, p);

        double meanY = 0;
        for (int i = 0; i < n; i++) meanY += Y[i];
        meanY /= n;

        double ssTot = 0, ssRes = 0;
        for (int i = 0; i < n; i++)
        {
            double yPred = 0;
            for (int j = 0; j < p; j++) yPred += X[i, j] * beta[j];
            ssRes += (Y[i] - yPred) * (Y[i] - yPred);
            ssTot += (Y[i] - meanY) * (Y[i] - meanY);
        }
        double r2 = ssTot > 1e-6 ? System.Math.Max(0.0, 1.0 - (ssRes / ssTot)) : 1.0;

        solvedWeights = new SolvedWeights
        {
            w_MaxHealth        = (float)beta[0],
            w_AttackDamage     = (float)beta[1],
            w_AttackFrequency  = (float)beta[2],
            w_MoveSpeed        = (float)beta[3],
            w_HorizontalRange  = (float)beta[4],
            w_KnockBackDamage  = (float)beta[5],
            w_KnockBackHealth  = (float)beta[6],
            rSquared           = (float)r2
        };

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>[BalanceSim] Calibration Complete (N={n}, R²={r2:F3})</b>");
        foreach (var r in results)
        {
            sb.AppendLine($"  • <b>{r.unitName}</b>: Killed {r.anchorsKilledEquivalent:F2} Anchors | CalcPower = {r.scaledCalculatedPower:F1} (SpawnCost = {(r.statsAsset != null ? r.statsAsset._spawnCost : 0)})");
        }
        Debug.Log(sb.ToString());
    }

    private double[] GaussianElimination(double[,] A, double[] b, int n)
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
    }
}
#endif