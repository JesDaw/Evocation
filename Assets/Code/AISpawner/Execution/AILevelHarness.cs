using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AIDecisionTrace; // [AI-INSPECTOR]

public class AILevelHarness : MonoBehaviour
{
    /// <summary>
    /// Gives the AI information about the map and handles AI spawning.
    /// </summary>
    public AIClan aiClan;
    int currentPhaseIndex = 0;

    public static AILevelHarness Instance { get; private set; }

    // [AI-INSPECTOR] read-only views for the Editor window (no behaviour change)
    public bool IsRunning => isRunning;
    public int CurrentPhaseIndex => currentPhaseIndex;
    public float VirtualBudget => moneyBudget;
    // Current activity-curve output (fraction of real income allocated to virtual budget).
    public float CurrentActivityBudgetPercent { get; private set; }

    public float tickInterval = 0.5f;

    [SerializeField] float maxThreatLevel = 100f;

    [Header("Experimental: Threat Activity Input")]
    [Tooltip("OFF: legacy clamp(|rawThreat| / maxThreatLevel). ON: use the zone-normalized relative military advantage in [-1,1] for activity-curve selection and input only. Requires UnitTracker zone-separated outcomes; candidate scoring remains untouched.")]
    [SerializeField] bool useRelativeThreatForActivity = false;
    [SerializeField] MapZone aiSpawnZone;

    [Header("AI Scoring & Multi-Spawn")]
    [SerializeField] float outcomeUtilityScale = 10f;
    [Tooltip("Maximum number of units the AI can spawn in a single tick when combining units inside its budget.")]
    [SerializeField, Range(1, 5)] int maxSpawnsPerTick = 3;
    [Tooltip("Time window (seconds) used to estimate how many copies of a cheap fast-cooldown unit can be deployed as a swarm.")]
    [SerializeField] float swarmEvaluationWindow = 4f;
    [Tooltip("Cap on how many virtual copies a single candidate can be evaluated as when swarming.")]
    [SerializeField] int maxSwarmCopies = 5;

    // [AI-INSPECTOR] was an inline literal (3f) in UpdateThreatAndBudget; value unchanged.
    const float ActivityCurveMultiplier = 3f;

    float threatLevel;
    float moneyBudget;

    bool isRunning;
    bool budgetInitialized;

    Coroutine AILoopCoroutine;
    CharacterCooldownHandler characterCooldownHandler;
    // Optional. When absent or disabled, all purchases follow the legacy evaluator.
    AITacticalPlanExecutor tacticalPlanExecutor;

    public bool showDebugLogs = false;

    // Read-only cooldown status for planning; execution remains owned by this harness.
    public float CooldownRemainingForPlan(ScriptableStats character)
    {
        return characterCooldownHandler != null
            ? characterCooldownHandler.CooldownRemaining(character)
            : 0f;
    }

    #region Startup

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        characterCooldownHandler = new CharacterCooldownHandler();
        tacticalPlanExecutor = GetComponent<AITacticalPlanExecutor>();
    }

    void Start()
    {
        if (!ValidateConfiguration())
            return;

        InitializeCurrentPhaseCooldowns();

        if (Money.AIInstance == null)
        {
            Debug.LogError("[AI] No AI Money instance found!");
            return;
        }

        InitializeBudget();
    }

    bool ValidateConfiguration()
    {
        if (aiClan == null)
        {
            Debug.LogError("[AI] No AIClan assigned!");
            return false;
        }

        if (aiClan.Phases == null || aiClan.Phases.Length == 0)
        {
            Debug.LogError("[AI] AIClan has no phases!");
            return false;
        }

        if (currentPhaseIndex < 0 ||
            currentPhaseIndex >= aiClan.Phases.Length)
        {
            Debug.LogError("[AI] Current phase index is invalid!");
            return false;
        }

        if (characterCooldownHandler == null)
            characterCooldownHandler = new CharacterCooldownHandler();

        return true;
    }

    void InitializeBudget()
    {
        if (budgetInitialized || Money.AIInstance == null)
            return;

        moneyBudget = Mathf.Max(0f, Money.AIInstance.CurrentMoney);
        budgetInitialized = true;
    }

    void InitializeCurrentPhaseCooldowns()
    {
        if (aiClan == null || aiClan.Phases == null ||
            currentPhaseIndex < 0 ||
            currentPhaseIndex >= aiClan.Phases.Length)
            return;

        ScriptableStats[] characters =
            aiClan.Phases[currentPhaseIndex].Characters;

        if (characters == null)
            return;

        foreach (ScriptableStats character in characters)
        {
            if (character == null)
                continue;

            if (!characterCooldownHandler.nextSpawnableTime.ContainsKey(character))
                characterCooldownHandler.nextSpawnableTime[character] = 0f;
        }
    }

    #endregion

    #region Phase Changing

    public void IncramentPhase()
    {
        if (aiClan == null || aiClan.Phases == null)
            return;

        if (currentPhaseIndex >= aiClan.Phases.Length - 1)
            return;

        currentPhaseIndex++;
        InitializeCurrentPhaseCooldowns();

        if (showDebugLogs)
            Debug.Log($"[AI] Advanced to phase {currentPhaseIndex}.");
    }

    public void ChangePhaseByName(string name)
    {
        if (aiClan == null || aiClan.Phases == null)
            return;

        for (int i = 0; i < aiClan.Phases.Length; i++)
        {
            if (aiClan.Phases[i].Name != name)
                continue;

            currentPhaseIndex = i;
            InitializeCurrentPhaseCooldowns();

            if (showDebugLogs)
                Debug.Log($"[AI] Changed to phase: {name}");

            return;
        }

        if (showDebugLogs)
            Debug.LogWarning($"[AI] Could not find phase named '{name}'.");
    }

    #endregion

    #region Starting and Stopping

    public void StartAI()
    {
        if (isRunning)
        {
            if (showDebugLogs)
                Debug.LogWarning("[AI] Already running!");

            return;
        }

        if (!ValidateConfiguration())
            return;

        if (Money.AIInstance == null)
        {
            Debug.LogError("[AI] Cannot start: AI Money instance is missing!");
            return;
        }

        if (UnitTracker.Instance == null)
        {
            Debug.LogError("[AI] Cannot start: UnitTracker instance is missing!");
            return;
        }

        if (SpawnObjects.EnemyInstance == null)
        {
            Debug.LogError("[AI] Cannot start: Enemy SpawnObjects instance is missing!");
            return;
        }

        InitializeBudget();
        InitializeCurrentPhaseCooldowns();

        isRunning = true;
        AILoopCoroutine = StartCoroutine(RunAILoop());

        if (showDebugLogs)
            Debug.Log("[AI] Started.");
    }

    IEnumerator RunAILoop()
    {
        while (isRunning)
        {
            if (showDebugLogs) Debug.Log("===================AI Tick Start========================");

            // [AI-INSPECTOR] null when recording is off -> every capture below is skipped
            AITickSnapshot snap = AIDecisionRecorder.BeginTick();
            if (snap != null) CaptureTickContext(snap);

            UpdateThreatAndBudget(snap);
            ExicuteBestAction(snap);

            AIDecisionRecorder.EndTick(snap); // [AI-INSPECTOR]

            yield return new WaitForSeconds(
                Mathf.Max(0.01f, tickInterval));
        }

        AILoopCoroutine = null;
    }

    public void StopAI()
    {
        isRunning = false;

        if (AILoopCoroutine != null)
        {
            StopCoroutine(AILoopCoroutine);
            AILoopCoroutine = null;
        }

        if (showDebugLogs)
            Debug.Log("[AI] Stopped.");
    }

    void OnDisable()
    {
        StopAI();
    }

    void OnDestroy()
    {
        StopAI();

        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region Budget and Threat

    void UpdateThreatAndBudget(AITickSnapshot snap) // [AI-INSPECTOR] added parameter (private method)
    {
        if (UnitTracker.Instance == null || Money.AIInstance == null)
        {
            if (snap != null) // [AI-INSPECTOR]
            {
                snap.updateStatus = AIUpdateStatus.SkippedMissingDependency;
                snap.updateProblem = DescribeMissingDependencies(false);
            }
            return;
        }

        if (!ValidateConfiguration())
        {
            if (snap != null) // [AI-INSPECTOR]
            {
                snap.updateStatus = AIUpdateStatus.SkippedInvalidConfig;
                snap.updateProblem = DescribeConfigProblem() ?? "Configuration validation failed.";
            }
            return;
        }

        CurrentActivityBudgetPercent = 0f;
        moneyBudget = Mathf.Clamp(
            moneyBudget,
            0f,
            Mathf.Max(0f, Money.AIInstance.CurrentMoney));

        if (snap != null) // [AI-INSPECTOR]
        {
            snap.actualMoneyAtUpdate = Money.AIInstance.CurrentMoney;
            snap.moneyGainPerSec = Money.AIInstance.MoneyGainPerSec;
            snap.virtualBudgetBeforeUpdate = moneyBudget;
        }

        threatLevel = UnitTracker.Instance.CalculateWeightedAIThreatLevel();

        float legacyRatio = maxThreatLevel > 0f
            ? Mathf.Abs(threatLevel) / maxThreatLevel
            : 0f;
        float relativeThreat = (useRelativeThreatForActivity || snap != null)
            ? UnitTracker.Instance.CalculateRelativeThreatForActivity()
            : 0f;
        // The experimental input is a bounded relative comparison, not a remapping
        // of the historical x100 threat. Its sign determines which curve to use.
        bool relativeModeActive = useRelativeThreatForActivity &&
                                  UnitTracker.Instance.ZoneSeparatedOutcomesEnabled;
        float activitySignedThreat = relativeModeActive ? relativeThreat : threatLevel;
        float clampedThreatRatio = relativeModeActive
            ? Mathf.Clamp01(Mathf.Abs(relativeThreat))
            : Mathf.Clamp01(legacyRatio);

        if (snap != null) // [AI-INSPECTOR]
        {
            snap.threatEvaluated = true;
            snap.rawThreat = threatLevel;
            snap.maxThreatLevel = maxThreatLevel;
            snap.unclampedThreatRatio = legacyRatio;
            snap.normalizedThreat = clampedThreatRatio;
            snap.legacyNormalizedThreat = Mathf.Clamp01(legacyRatio);
            snap.relativeMilitaryAdvantage = UnitTracker.Instance.CalculateRelativeMilitaryAdvantage();
            snap.relativeThreatForActivity = relativeThreat;
            snap.relativeActivityModeEnabled = relativeModeActive;
            snap.militaryAdvantage = UnitTracker.Instance.CalculateMilitaryAdvantage();
            snap.defensiveUrgencyPressure = UnitTracker.Instance.CalculateDefensiveUrgencyPressure();
        }

        var phase = aiClan.Phases[currentPhaseIndex];

        if (phase.AdvantageActivityCurve == null ||
            phase.DisadvantageActivityCurve == null)
        {
            if (snap != null) snap.updateStatus = AIUpdateStatus.SkippedMissingActivityCurve; // [AI-INSPECTOR]
            Debug.LogWarning("[AI] Current phase has a missing activity curve.");
            return;
        }

        // [AI-INSPECTOR] Same arithmetic as before ((cond ? A.Evaluate(x) * 3f : B.Evaluate(x) * 3f));
        // the raw curve value is now kept in a local so it can be recorded.
        bool useDisadvantageCurve = activitySignedThreat >= 0f;
        float curveValue = useDisadvantageCurve
            ? phase.DisadvantageActivityCurve.Evaluate(clampedThreatRatio)
            : phase.AdvantageActivityCurve.Evaluate(clampedThreatRatio);
        float budgetPercentThisTick = curveValue * ActivityCurveMultiplier;

        budgetPercentThisTick = Mathf.Max(0f, budgetPercentThisTick);
        CurrentActivityBudgetPercent = budgetPercentThisTick;

        float incomeThisTick = Money.AIInstance.MoneyGainPerSec *
                               Mathf.Max(0f, tickInterval);

        float budgetAdded = incomeThisTick * budgetPercentThisTick;

        moneyBudget = Mathf.Clamp(
            moneyBudget + budgetAdded,
            0f,
            Mathf.Max(0f, Money.AIInstance.CurrentMoney));

        if (snap != null) // [AI-INSPECTOR]
        {
            snap.updateStatus = AIUpdateStatus.Ok;
            snap.curveKind = useDisadvantageCurve ? AIActivityCurveKind.Disadvantage : AIActivityCurveKind.Advantage;
            snap.curveValue = curveValue;
            snap.activityMultiplier = ActivityCurveMultiplier;
            snap.activityBudgetPercent = budgetPercentThisTick;
            snap.incomeThisTick = incomeThisTick;
            snap.budgetAdded = budgetAdded;
            snap.virtualBudgetAfterUpdate = moneyBudget;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"[AI] Threat: {threatLevel:F2} | " +
                $"Activity: {budgetPercentThisTick:F2} | " +
                $"Budget added: {budgetAdded:F2} | " +
                $"Virtual budget: {moneyBudget:F2} ({budgetPercentThisTick}% of incomeThisTick ({incomeThisTick}))| " +
                $"Actual money: {Money.AIInstance.CurrentMoney:F2}");
        }
    }

    #endregion

    #region Decision Making

    void ExicuteBestAction(AITickSnapshot snap) // [AI-INSPECTOR] added parameter (private method)
    {
        if (!ValidateConfiguration() ||
            UnitTracker.Instance == null ||
            Money.AIInstance == null ||
            SpawnObjects.EnemyInstance == null)
        {
            if (snap != null) // [AI-INSPECTOR]
            {
                string cfgProblem = DescribeConfigProblem();
                if (cfgProblem != null)
                {
                    snap.executeStatus = AIExecuteStatus.SkippedInvalidConfig;
                    snap.executeProblem = cfgProblem;
                }
                else
                {
                    snap.executeStatus = AIExecuteStatus.SkippedMissingDependency;
                    snap.executeProblem = DescribeMissingDependencies(true);
                }
            }
            return;
        }

        ScriptableStats[] characters =
            aiClan.Phases[currentPhaseIndex].Characters;

        if (characters == null || characters.Length == 0)
        {
            if (snap != null) snap.executeStatus = AIExecuteStatus.NoCharactersInPhase; // [AI-INSPECTOR]
            return;
        }

        int safeMaxSpawns = Mathf.Max(1, maxSpawnsPerTick);

        for (int spawnPass = 0; spawnPass < safeMaxSpawns; spawnPass++)
        {
            moneyBudget = Mathf.Clamp(
                moneyBudget,
                0f,
                Mathf.Max(0f, Money.AIInstance.CurrentMoney));

            AIPassSnapshot pass = null; // [AI-INSPECTOR]
            if (snap != null)
            {
                pass = new AIPassSnapshot
                {
                    passIndex = spawnPass,
                    actualMoneyBefore = Money.AIInstance.CurrentMoney,
                    virtualBudgetBefore = moneyBudget
                };
                snap.passes.Add(pass);
            }

            // The plan manager supplies a target but never buys it. We intentionally
            // evaluate only that target in plan mode so the existing savings,
            // cooldown, wallet and spawn path remains the single executor.
            // In legacy mode the complete phase roster is still evaluated.
            if (tacticalPlanExecutor == null)
                tacticalPlanExecutor = GetComponent<AITacticalPlanExecutor>();

            ScriptableStats plannedTarget = null;
            bool planControlsPass = tacticalPlanExecutor != null &&
                tacticalPlanExecutor.TryGetTarget(this, currentPhaseIndex,
                    characters, out plannedTarget);

            // A null planned target means the plan has finished or is intentionally
            // waiting for rearm. Do not fill this gap with unrelated purchases.
            if (planControlsPass && plannedTarget == null)
            {
                if (snap != null)
                {
                    snap.passes.Remove(pass);
                    snap.executeStatus = AIExecuteStatus.RanPasses;
                    snap.tickEnd = AITickEnd.PassEndedTick;
                    snap.virtualBudgetAtEnd = moneyBudget;
                    snap.actualMoneyAtEnd = Money.AIInstance.CurrentMoney;
                }
                return;
            }

            ScriptableStats[] evaluationRoster = planControlsPass
                ? new ScriptableStats[] { plannedTarget }
                : characters;

            CharacterUtility bestCharacter = SelectBestCandidate(evaluationRoster, pass);
            if (bestCharacter == null)
            {
                RecordPassEnd(snap, pass, AIPassOutcome.NoValidCandidates, AITickEnd.PassEndedTick); // [AI-INSPECTOR]
                return;
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[AI] Pass {spawnPass + 1} Highest utility: {bestCharacter.totalUtility:F3} " +
                    $"({bestCharacter.character.name}, SwarmEval={bestCharacter.evaluatedCopies}x)");
            }

            // If the best choice is over our current virtual budget, save up for it (and upgrade wallet if possible)
            // rather than spending the budget on worse units.
            if (bestCharacter.overBudget)
            {
                if (spawnPass == 0)
                {
                    int tierBefore = pass != null ? Money.AIInstance.CurrentMaxMoneyIndex : 0; // [AI-INSPECTOR]

                    Money.AIInstance.UpgradeMaxMoney();

                    if (pass != null) // [AI-INSPECTOR]
                    {
                        pass.walletUpgradeAttempted = true;
                        pass.walletUpgraded = Money.AIInstance.CurrentMaxMoneyIndex != tierBefore;
                    }
                }

                if (showDebugLogs)
                    Debug.Log($"[AI] Saving for '{bestCharacter.character.name}'.");

                RecordPassEnd(snap, pass, AIPassOutcome.SavingOverBudget, AITickEnd.PassEndedTick); // [AI-INSPECTOR]
                return;
            }

            // If the winning choice is still on spawn cooldown, wait for it rather than spending money elsewhere.
            if (pass != null) // [AI-INSPECTOR]
                pass.cooldownRemainingAtDecision = characterCooldownHandler.CooldownRemaining(bestCharacter.character);

            if (characterCooldownHandler.CooldownRemaining(bestCharacter.character) > 0f)
            {
                if (showDebugLogs)
                    Debug.Log($"[AI] Waiting on cooldown for '{bestCharacter.character.name}'.");

                RecordPassEnd(snap, pass, AIPassOutcome.WaitingOnCooldown, AITickEnd.PassEndedTick); // [AI-INSPECTOR]
                return;
            }

            GameObject spawned =
                SpawnObjects.EnemyInstance.Spawn(bestCharacter.character);

            if (spawned == null)
            {
                if (planControlsPass)
                    tacticalPlanExecutor.NotifySpawnFailure(bestCharacter.character);
                RecordPassEnd(snap, pass, AIPassOutcome.SpawnFailed, AITickEnd.PassEndedTick); // [AI-INSPECTOR]
                return;
            }

            characterCooldownHandler.StartCooldown(bestCharacter.character);
            if (planControlsPass)
                tacticalPlanExecutor.NotifySuccessfulPurchase(bestCharacter.character);

            moneyBudget = Mathf.Max(
                0f,
                moneyBudget - bestCharacter.character._spawnCost);

            // Immediately register the newly spawned unit into aiSpawnZone so any subsequent
            // combo spawn pass in this same tick sees the updated AI frontline!
            if (aiSpawnZone != null)
                aiSpawnZone.SendMessage("AddCharacterToList", spawned, SendMessageOptions.DontRequireReceiver);

            if (pass != null) // [AI-INSPECTOR]
            {
                pass.spawnedObjectName = spawned.name;
                pass.spawnZoneNotified = aiSpawnZone != null;
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[AI] Spawned: {bestCharacter.character.name} | " +
                    $"Remaining virtual budget: {moneyBudget:F2}");
            }

            // A completed two-unit plan must not spawn a third unit on another
            // pass of the same tick, regardless of remaining virtual budget.
            if (planControlsPass && tacticalPlanExecutor.FinishedCurrentPlan)
            {
                RecordPassEnd(snap, pass, AIPassOutcome.Spawned, AITickEnd.PassEndedTick);
                return;
            }

            if (moneyBudget <= 0f)
            {
                RecordPassEnd(snap, pass, AIPassOutcome.Spawned, AITickEnd.BudgetExhausted); // [AI-INSPECTOR]
                return;
            }

            // [AI-INSPECTOR] provisional: overwritten by the next pass if the loop continues
            RecordPassEnd(snap, pass, AIPassOutcome.Spawned, AITickEnd.MaxPassesReached);
        }
    }

    
    CharacterUtility SelectBestCandidate(
        ScriptableStats[] characters,
        AIPassSnapshot pass)
    {
        var evaluator = new AIUnitEvaluator(
            characterCooldownHandler,
            aiSpawnZone,
            outcomeUtilityScale,
            swarmEvaluationWindow,
            maxSwarmCopies,
            showDebugLogs);

        return evaluator.SelectBestCandidate(
            characters,
            pass,
            moneyBudget);
    }


    int CalculateCandidateSwarmCopies(float cost, float cooldown)
    {
        if (cost <= 0f || moneyBudget < cost)
            return 1;

        int affordableCopies = Mathf.FloorToInt(moneyBudget / cost);
        int cooldownCopies = cooldown <= 0.05f
            ? maxSwarmCopies
            : 1 + Mathf.FloorToInt(Mathf.Max(0f, swarmEvaluationWindow) / cooldown);

        return Mathf.Clamp(
            Mathf.Min(affordableCopies, cooldownCopies),
            1,
            Mathf.Max(1, maxSwarmCopies));
    }

    #endregion

    #region AI Inspector instrumentation helpers

    // [AI-INSPECTOR] Everything in this region is only reached when a snapshot exists (recording on).

    void CaptureTickContext(AITickSnapshot snap)
    {
        snap.tickInterval = tickInterval;
        snap.maxThreatLevel = maxThreatLevel;
        snap.outcomeUtilityScale = outcomeUtilityScale;
        snap.maxSpawnsPerTick = maxSpawnsPerTick;
        snap.swarmEvaluationWindow = swarmEvaluationWindow;
        snap.maxSwarmCopies = maxSwarmCopies;
        snap.aiSpawnZoneAssigned = aiSpawnZone != null;
        snap.phaseIndex = currentPhaseIndex;

        if (aiClan != null && aiClan.Phases != null)
        {
            snap.phaseCount = aiClan.Phases.Length;
            if (currentPhaseIndex >= 0 && currentPhaseIndex < aiClan.Phases.Length && aiClan.Phases[currentPhaseIndex] != null)
            {
                AIClanPhase phase = aiClan.Phases[currentPhaseIndex];
                snap.phaseName = phase.Name;
                snap.phaseCharacterCount = phase.Characters != null ? phase.Characters.Length : 0;
            }
        }

        // [AI-INSPECTOR 1.5] one-off session context (scene, clan, phase roster, settings)
        if (AIDecisionRecorder.Session == null)
            AIDecisionRecorder.Session = BuildSessionMetadata();
    }

    AISessionMetadata BuildSessionMetadata()
    {
        AISessionMetadata m = new AISessionMetadata
        {
            sceneName = gameObject.scene.name,
            aiClanName = aiClan != null ? aiClan.name : "(none)",
            aiSpawnZoneName = aiSpawnZone != null ? aiSpawnZone.name : "(none)",
            unityVersion = Application.unityVersion,
            startedUtc = System.DateTime.UtcNow.ToString("o"),
            startGameTime = Time.time,
            startFrame = Time.frameCount,
            timeScale = Time.timeScale,
            tickInterval = tickInterval,
            maxThreatLevel = maxThreatLevel,
            outcomeUtilityScale = outcomeUtilityScale,
            maxSpawnsPerTick = maxSpawnsPerTick,
            swarmEvaluationWindow = swarmEvaluationWindow,
            maxSwarmCopies = maxSwarmCopies
        };

        if (aiClan != null && aiClan.Phases != null)
        {
            for (int i = 0; i < aiClan.Phases.Length; i++)
            {
                AIClanPhase phase = aiClan.Phases[i];
                if (phase == null) continue;

                AISessionPhase sp = new AISessionPhase { index = i, name = phase.Name };
                if (phase.Characters != null)
                    foreach (ScriptableStats c in phase.Characters)
                        sp.unitNames.Add(c != null ? c.name : "(null)");
                m.phases.Add(sp);
            }
        }
        return m;
    }

    void RecordPassEnd(AITickSnapshot snap, AIPassSnapshot pass, AIPassOutcome outcome, AITickEnd tickEnd)
    {
        if (snap == null || pass == null) return;

        int actual = Money.AIInstance != null ? Money.AIInstance.CurrentMoney : 0;

        pass.outcome = outcome;
        pass.virtualBudgetAfter = moneyBudget;
        pass.actualMoneyAfter = actual;

        snap.tickEnd = tickEnd;
        snap.virtualBudgetAtEnd = moneyBudget;
        snap.actualMoneyAtEnd = actual;
    }

    string DescribeMissingDependencies(bool includeSpawner)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (UnitTracker.Instance == null) sb.Append("UnitTracker.Instance is null. ");
        if (Money.AIInstance == null) sb.Append("Money.AIInstance is null. ");
        if (includeSpawner && SpawnObjects.EnemyInstance == null) sb.Append("SpawnObjects.EnemyInstance is null. ");
        return sb.ToString().Trim();
    }

    /// <summary>Returns null when the configuration is valid.</summary>
    string DescribeConfigProblem()
    {
        if (aiClan == null) return "aiClan is not assigned.";
        if (aiClan.Phases == null || aiClan.Phases.Length == 0) return "AIClan has no phases.";
        if (currentPhaseIndex < 0 || currentPhaseIndex >= aiClan.Phases.Length)
            return $"Current phase index {currentPhaseIndex} is out of range (0..{aiClan.Phases.Length - 1}).";
        return null;
    }

    #endregion
}

public class CharacterCooldownHandler
{
    public Dictionary<ScriptableStats, float> nextSpawnableTime =
        new Dictionary<ScriptableStats, float>();

    public void StartCooldown(ScriptableStats character)
    {
        if (character == null)
            return;

        nextSpawnableTime[character] =
            Time.time + Mathf.Max(0f, character._spawnCooldown);
    }

    public float CooldownRemaining(ScriptableStats character)
    {
        if (character == null)
            return 0f;

        if (!nextSpawnableTime.TryGetValue(character, out float readyTime))
            return 0f;

        return Mathf.Max(0f, readyTime - Time.time);
    }
}

public class CharacterUtility
{
    public ScriptableStats character;
    public int evaluatedCopies = 1;
    public float moneyUtility;
    public float spawnCooldownUtility;
    public float expendatureRateUtility;
    public bool overBudget;
    public float expectedOutcomeRaw;
    public float expectedOutcomeUtility;
    public float totalUtility;

    public void Print(float moneyBudget)
    {
        Debug.Log($"====== Utilities for {character.name} (Swarm: {evaluatedCopies}x) ======");
        Debug.Log(
            $"Money: {moneyUtility:F3} | " +
            $"Cooldown: {spawnCooldownUtility:F3} | " +
            $"Expenditure: {expendatureRateUtility:F3} | " +
            $"Outcome utility: {expectedOutcomeUtility:F3} (Outcome delta: {expectedOutcomeRaw:F3})");

        Debug.Log(
            $"Total utility: {totalUtility:F3} | " +
            $"Cost: {character._spawnCost} | " +
            $"Virtual budget: {moneyBudget:F2} | " +
            $"afordable: {!overBudget}");
    }
}