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

    public float tickInterval = 0.5f;

    [SerializeField] float maxThreatLevel = 100f;
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

    public bool showDebugLogs = false;

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

        float clampedThreatRatio = maxThreatLevel > 0f
            ? Mathf.Clamp01(Mathf.Abs(threatLevel) / maxThreatLevel)
            : 0f;

        if (snap != null) // [AI-INSPECTOR]
        {
            snap.threatEvaluated = true;
            snap.rawThreat = threatLevel;
            snap.maxThreatLevel = maxThreatLevel;
            snap.unclampedThreatRatio = maxThreatLevel > 0f ? Mathf.Abs(threatLevel) / maxThreatLevel : 0f;
            snap.normalizedThreat = clampedThreatRatio;
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
        bool useDisadvantageCurve = threatLevel >= 0f;
        float curveValue = useDisadvantageCurve
            ? phase.DisadvantageActivityCurve.Evaluate(clampedThreatRatio)
            : phase.AdvantageActivityCurve.Evaluate(clampedThreatRatio);
        float budgetPercentThisTick = curveValue * ActivityCurveMultiplier;

        budgetPercentThisTick = Mathf.Max(0f, budgetPercentThisTick);

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

            CharacterUtility bestCharacter = SelectBestCandidate(characters, pass);
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
                RecordPassEnd(snap, pass, AIPassOutcome.SpawnFailed, AITickEnd.PassEndedTick); // [AI-INSPECTOR]
                return;
            }

            characterCooldownHandler.StartCooldown(bestCharacter.character);

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

            if (moneyBudget <= 0f)
            {
                RecordPassEnd(snap, pass, AIPassOutcome.Spawned, AITickEnd.BudgetExhausted); // [AI-INSPECTOR]
                return;
            }

            // [AI-INSPECTOR] provisional: overwritten by the next pass if the loop continues
            RecordPassEnd(snap, pass, AIPassOutcome.Spawned, AITickEnd.MaxPassesReached);
        }
    }

    CharacterUtility SelectBestCandidate(ScriptableStats[] characters, AIPassSnapshot pass) // [AI-INSPECTOR] added parameter (private method)
    {
        List<CharacterUtility> characterUtilities = new List<CharacterUtility>();

        float currentOutcome =
            UnitTracker.Instance.CalculateOverallExpectedOutcome(AIWeighted: true);

        if (pass != null) pass.baselineOutcome = currentOutcome; // [AI-INSPECTOR]

        float maxAbsOutcomeDelta = 0f;

        // Pass 1: Compute raw outcome deltas and non-outcome utilities
        foreach (ScriptableStats character in characters)
        {
            if (character == null)
            {
                if (pass != null) pass.nullCharacterEntries++; // [AI-INSPECTOR]
                continue;
            }

            float cost = character._spawnCost;
            float cooldown = character._spawnCooldown;

            if (float.IsNaN(cost) || float.IsInfinity(cost) ||
                float.IsNaN(cooldown) || float.IsInfinity(cooldown) ||
                cost < 0f || cooldown < 0f)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[AI] Skipping invalid candidate: {character.name}");

                if (pass != null) // [AI-INSPECTOR]
                    pass.skippedCandidates.Add($"{character.name} (cost={cost}, cooldown={cooldown})");

                continue;
            }

            var cu = new CharacterUtility { character = character };

            cu.moneyUtility = cost <= 0f
                ? 1f
                : Mathf.Clamp01((float)Money.AIInstance.CurrentMoney / cost);

            cu.spawnCooldownUtility = cooldown <= 0f
                ? 1f
                : Mathf.Clamp01(
                    1f - characterCooldownHandler.CooldownRemaining(character) / cooldown);

            int swarmCopies = CalculateCandidateSwarmCopies(cost, cooldown);
            cu.evaluatedCopies = swarmCopies;

            float committedCost = cost * swarmCopies;
            cu.expendatureRateUtility = moneyBudget > 0f
                ? Mathf.Clamp01(committedCost / moneyBudget)
                : 0f;

            cu.overBudget = cost > moneyBudget;

            float outcomeWithCandidate =
                UnitTracker.Instance.CalculateOverallExpectedOutcome(
                    character,
                    relaventZone: aiSpawnZone,
                    AIWeighted: true,
                    extraCharacterCopies: swarmCopies);

            cu.expectedOutcomeRaw = currentOutcome - outcomeWithCandidate;

            float absDelta = Mathf.Abs(cu.expectedOutcomeRaw);
            if (absDelta > maxAbsOutcomeDelta)
                maxAbsOutcomeDelta = absDelta;

            // [AI-INSPECTOR] Capture the values computed above. Must stay adjacent to
            // characterUtilities.Add(cu) so pass.candidates[i] corresponds to characterUtilities[i].
            if (pass != null)
            {
                pass.candidates.Add(new AICandidateSnapshot
                {
                    unitName = character.name,
                    unitAsset = character,
                    spawnCost = cost,
                    spawnCooldown = cooldown,
                    cooldownRemaining = characterCooldownHandler.CooldownRemaining(character), // same frame => same value used above
                    actualMoney = Money.AIInstance.CurrentMoney,
                    virtualBudget = moneyBudget,
                    affordableActual = Money.AIInstance.CurrentMoney >= cost,
                    affordableVirtual = !cu.overBudget,
                    swarmCopies = swarmCopies,
                    committedCost = committedCost,
                    moneyUtility = cu.moneyUtility,
                    cooldownUtility = cu.spawnCooldownUtility,
                    expenditureUtility = cu.expendatureRateUtility,
                    baselineOutcome = currentOutcome,
                    outcomeWithCandidate = outcomeWithCandidate,
                    outcomeRaw = cu.expectedOutcomeRaw
                });
            }

            characterUtilities.Add(cu);
        }

        if (characterUtilities.Count == 0)
            return null;

        // Pass 2: Normalize expectedOutcomeUtility against the best candidate delta (with outcomeUtilityScale as a minimum floor)
        float outcomeDenominator = Mathf.Max(maxAbsOutcomeDelta, outcomeUtilityScale, 0.001f);

        foreach (CharacterUtility cu in characterUtilities)
        {
            cu.expectedOutcomeUtility = Mathf.Clamp(
                cu.expectedOutcomeRaw / outcomeDenominator,
                -1f,
                1f);

            cu.totalUtility = (
                cu.moneyUtility +
                cu.spawnCooldownUtility +
                cu.expendatureRateUtility +
                cu.expectedOutcomeUtility
            ) / 4f;

            if (showDebugLogs) cu.Print(moneyBudget);
        }

        // [AI-INSPECTOR] was: return characterUtilities.OrderByDescending(cu => cu.totalUtility).First();
        CharacterUtility best = characterUtilities
            .OrderByDescending(cu => cu.totalUtility)
            .First();

        if (pass != null) // [AI-INSPECTOR]
        {
            pass.maxAbsOutcomeDelta = maxAbsOutcomeDelta;
            pass.outcomeDenominator = outcomeDenominator;

            for (int i = 0; i < characterUtilities.Count; i++)
            {
                CharacterUtility util = characterUtilities[i];
                AICandidateSnapshot s = pass.candidates[i];
                s.outcomeUtility = util.expectedOutcomeUtility;
                s.totalUtility = util.totalUtility;
            }

            int bestIndex = characterUtilities.IndexOf(best);
            pass.selectedIndex = bestIndex;
            if (bestIndex >= 0) pass.candidates[bestIndex].isSelected = true;
            pass.AssignRanks();
        }

        return best;
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