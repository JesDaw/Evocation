
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AILevelHarness : MonoBehaviour
{
    /// <summary>
    /// Gives the AI information about the map and handles AI spawning.
    /// </summary>
    public AIClan aiClan;
    int currentPhaseIndex = 0;

    public static AILevelHarness Instance { get; private set; }

    public float tickInterval = 0.5f;

    [SerializeField] float maxThreatLevel = 100f;
    [SerializeField] MapZone aiSpawnZone;

    [Header("AI Scoring")]
    [SerializeField] float outcomeUtilityScale = 10f;

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

            // Don't reset an existing cooldown during a phase change.
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
            UpdateThreatAndBudget();
            ExicuteBestAction();

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

    void UpdateThreatAndBudget()
    {
        if (UnitTracker.Instance == null || Money.AIInstance == null)
            return;

        if (!ValidateConfiguration())
            return;

        // The allowance cannot exceed the money currently available.
        moneyBudget = Mathf.Clamp(
            moneyBudget,
            0f,
            Mathf.Max(0f, Money.AIInstance.CurrentMoney));

        threatLevel = UnitTracker.Instance.CalculateWeightedAIThreatLevel();

        float clampedThreatRatio = maxThreatLevel > 0f
            ? Mathf.Clamp01(Mathf.Abs(threatLevel) / maxThreatLevel)
            : 0f;

        var phase = aiClan.Phases[currentPhaseIndex];

        if (phase.AdvantageActivityCurve == null ||
            phase.DisadvantageActivityCurve == null)
        {
            Debug.LogWarning(
                "[AI] Current phase has a missing activity curve.");
            return;
        }

        float budgetPercentThisTick = (threatLevel >= 0f)
            ? phase.DisadvantageActivityCurve.Evaluate(clampedThreatRatio) * 3f
            : phase.AdvantageActivityCurve.Evaluate(clampedThreatRatio) * 3f;

        budgetPercentThisTick = Mathf.Max(0f, budgetPercentThisTick);

        float incomeThisTick = Money.AIInstance.MoneyGainPerSec *
                               Mathf.Max(0f, tickInterval);

        float budgetAdded = incomeThisTick * budgetPercentThisTick;

        moneyBudget = Mathf.Clamp(
            moneyBudget + budgetAdded,
            0f,
            Mathf.Max(0f, Money.AIInstance.CurrentMoney));

        if (showDebugLogs)
        {
            Debug.Log(
                $"[AI] Threat: {threatLevel:F2} | " +
                $"Activity: {budgetPercentThisTick:F2} | " +
                $"Budget added: {budgetAdded:F2} | " +
                $"Virtual budget: {moneyBudget:F2} | " +
                $"Actual money: {Money.AIInstance.CurrentMoney:F2}");
        }
    }

    #endregion

    #region Decision Making

    void ExicuteBestAction()
    {
        if (!ValidateConfiguration() ||
            UnitTracker.Instance == null ||
            Money.AIInstance == null ||
            SpawnObjects.EnemyInstance == null)
            return;

        ScriptableStats[] characters =
            aiClan.Phases[currentPhaseIndex].Characters;

        if (characters == null || characters.Length == 0)
            return;

        // Reconcile the allowance with available money before scoring.
        moneyBudget = Mathf.Clamp(
            moneyBudget,
            0f,
            Mathf.Max(0f, Money.AIInstance.CurrentMoney));

        List<CharacterUtility> characterUtilities =
            new List<CharacterUtility>();

        float currentOutcome =
            UnitTracker.Instance.CalculateOverallExpectedOutcome(
                AIWeighted: true);

        foreach (ScriptableStats character in characters)
        {
            if (character == null)
                continue;

            float cost = character._spawnCost;
            float cooldown = character._spawnCooldown;

            if (float.IsNaN(cost) || float.IsInfinity(cost) ||
                float.IsNaN(cooldown) || float.IsInfinity(cooldown) ||
                cost < 0f || cooldown < 0f)
            {
                if (showDebugLogs)
                    Debug.LogWarning(
                        $"[AI] Skipping invalid candidate: {character.name}");

                continue;
            }

            var cu = new CharacterUtility { character = character };

            cu.moneyUtility = cost <= 0f
                ? 1f
                : Mathf.Clamp01(
                    (float)Money.AIInstance.CurrentMoney / cost);

            cu.spawnCooldownUtility = cooldown <= 0f
                ? 1f
                : Mathf.Clamp01(
                    1f -
                    characterCooldownHandler.CooldownRemaining(character) /
                    cooldown);

            cu.expendatureRateUtility = moneyBudget > 0f
                ? Mathf.Clamp01(cost / moneyBudget)
                : 0f;

            // Keep expensive units eligible so the AI can save for them.
            cu.overBudget = cost > moneyBudget;

            float outcomeWithCandidate =
                UnitTracker.Instance.CalculateOverallExpectedOutcome(
                    character,
                    AIWeighted: true,
                    relaventZone: aiSpawnZone);

            cu.expectedOutcomeRaw =
                currentOutcome - outcomeWithCandidate;

            cu.expectedOutcomeUtility = Mathf.Clamp(
                cu.expectedOutcomeRaw /
                Mathf.Max(outcomeUtilityScale, 0.001f),
                -1f,
                1f);

            cu.totalUtility = (
                cu.moneyUtility +
                cu.spawnCooldownUtility +
                cu.expendatureRateUtility +
                cu.expectedOutcomeUtility
            ) / 4f;

            characterUtilities.Add(cu);

            if (showDebugLogs)
                cu.Print(moneyBudget);
        }

        if (characterUtilities.Count == 0)
            return;

        CharacterUtility bestCharacter =
            characterUtilities
                .OrderByDescending(cu => cu.totalUtility)
                .First();

        if (showDebugLogs)
        {
            Debug.Log(
                $"[AI] Highest utility: {bestCharacter.totalUtility:F3} " +
                $"({bestCharacter.character.name})");
        }

        if (bestCharacter.overBudget)
        {
            Money.AIInstance.UpgradeMaxMoney();

            if (showDebugLogs)
                Debug.Log(
                    $"[AI] Saving for '{bestCharacter.character.name}'.");

            return;
        }

        GameObject spawned =
            SpawnObjects.EnemyInstance.Spawn(bestCharacter.character);

        if (spawned != null)
        {
            characterCooldownHandler.StartCooldown(
                bestCharacter.character);

            moneyBudget = Mathf.Max(
                0f,
                moneyBudget - bestCharacter.character._spawnCost);
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"[AI] Spawn attempt: {bestCharacter.character.name} | " +
                $"Success: {spawned != null} | " +
                $"Virtual budget: {moneyBudget:F2}");
        }
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

        if (!nextSpawnableTime.TryGetValue(
                character, out float readyTime))
            return 0f;

        return Mathf.Max(0f, readyTime - Time.time);
    }
}

public class CharacterUtility
{
    public ScriptableStats character;
    public float moneyUtility;
    public float spawnCooldownUtility;
    public float expendatureRateUtility;
    public bool overBudget;
    public float expectedOutcomeRaw;
    public float expectedOutcomeUtility;
    public float totalUtility;

    public void Print(float moneyBudget)
    {
        Debug.Log($"====== Utilities for {character.name} ======");
        Debug.Log(
            $"Money: {moneyUtility:F3} | " +
            $"Cooldown: {spawnCooldownUtility:F3} | " +
            $"Expenditure: {expendatureRateUtility:F3}");

        Debug.Log(
            $"Outcome delta: {expectedOutcomeRaw:F3} | " +
            $"Outcome utility: {expectedOutcomeUtility:F3}");

        Debug.Log(
            $"Total utility: {totalUtility:F3} | " +
            $"Cost: {character._spawnCost} | " +
            $"Virtual budget: {moneyBudget:F2} | " +
            $"Over budget: {overBudget}");
    }
}
