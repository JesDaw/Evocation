using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AILevelHarness : MonoBehaviour
{
    /// <summary>
    /// this just give the AI all the information on the map it needs and helps it exicute stuff
    /// this deals with AI spawning
    /// </summary>
    public AIClan aiClan;
    int currentPhaseIndex = 0;
    public static AILevelHarness Instance { get; private set; }
    public float tickInterval = 0.5f;
    [SerializeField] float maxThreatLevel = 100;
    [SerializeField] MapZone aiSpawnZone;
    float threatLevel;
    float moneyBudget;

    bool isRunning = false;
    Coroutine AILoopCoroutine;
    CharacterCooldownHandler characterCooldownHandler;
    public bool showDebugLogs = false;

    #region startup

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        characterCooldownHandler = new CharacterCooldownHandler();
        foreach (var c in aiClan.Phases[currentPhaseIndex].Characters) characterCooldownHandler.nextSpawnableTime[c] = 0f;
    }

    private void Start()
    {
        if (aiClan == null)
        {
            Debug.LogError("[AI] No AIClan assigned!");
            return;
        }
        if (aiClan.Phases == null || aiClan.Phases.Length == 0)
        {
            Debug.LogError("[AI] AIClan has no Phases!");
        }
        moneyBudget = Money.AIInstance.CurrentMoney;
    }
    #endregion
    #region Phasechanging
    public void IncramentPhase()
    {
        if (currentPhaseIndex >= aiClan.Phases.Length) return;
        currentPhaseIndex++;
        if (showDebugLogs) Debug.Log($"[AI] Phase advanced. CurrentPhaseIndex: {currentPhaseIndex}");
    }

    public void ChangePhaseByName(string name)
    {
        for (int i = 0; i < aiClan.Phases.Length; i++)
        {
            if (aiClan.Phases[i].Name == name)
            {
                currentPhaseIndex = i;
                break;
            }
        }
    }
    #endregion
    #region starting and stopping
    public void StartAI()
    {
        if (showDebugLogs) Debug.Log($"Starting AI");
        if (isRunning)
        {
            Debug.LogWarning("[AI] Already running!");
            return;
        }
        if (aiClan == null)
        {
            Debug.LogError("[AI] Cannot start: No AIClan assigned!");
            return;
        }

        isRunning = true;
        AILoopCoroutine = StartCoroutine(RunAILoop());
    }
    IEnumerator RunAILoop()
    {
        while (isRunning)
        {
            if (showDebugLogs) Debug.Log($"==================================Starting new AI Desision================================");
            UpdateThreatAndBudget();
            ExicuteBestAction();
            if (showDebugLogs) Debug.Log($"==================================End utility loop================================");

            yield return new WaitForSeconds(tickInterval);
        }
    }
    public void StopAI()
    {
        if (showDebugLogs) Debug.Log($"Stopping AI");
        isRunning = false;
        if (AILoopCoroutine != null) StopCoroutine(AILoopCoroutine);
    }
    #endregion

    void UpdateThreatAndBudget()
    {
        threatLevel = UnitTracker.Instance.CalculateWeightedAIThreatLevel();
        if (showDebugLogs) Debug.Log($"threatLevel (positive means Ai is loosing): {threatLevel} out of {maxThreatLevel}");

        float clampedThreatRatio = Mathf.Clamp(threatLevel / maxThreatLevel, 0f, 1f);
        float budgetPercentThisTick = (threatLevel >= 0)
            ? aiClan.Phases[currentPhaseIndex].DisadvantageActivityCurve.Evaluate(clampedThreatRatio) * 3
            : aiClan.Phases[currentPhaseIndex].AdvantageActivityCurve.Evaluate(clampedThreatRatio) * 3;

        float incomeThisTick = Money.AIInstance.MoneyGainPerSec * tickInterval;
        float budgetAdded = incomeThisTick * budgetPercentThisTick;
        moneyBudget = Mathf.Clamp(moneyBudget + budgetAdded , 0f, Money.AIInstance.CurrentMoney);
        

        if (showDebugLogs) Debug.Log($"incomeThisTick: {incomeThisTick}, budgetPercentThisTick: {budgetPercentThisTick}, budgetAdded: {budgetAdded}, moneyBudget now: {moneyBudget}");
    }

    void ExicuteBestAction()
    {
        List<CharacterUtility> characterUtilities = new List<CharacterUtility>();

        float totalExpectedOutcome = UnitTracker.Instance.CalculateOverallExpectedOutcome(AIWeighted: true);

        foreach (ScriptableStats character in aiClan.Phases[currentPhaseIndex].Characters)
        {
            var cu = new CharacterUtility { character = character };

            cu.moneyUtility = Mathf.Clamp((float)Money.AIInstance.CurrentMoney / character._spawnCost, 0f, 1f);
            cu.spawnCooldownUtility = 1f - (characterCooldownHandler.CooldownRemaining(character) / character._spawnCooldown);

            // Reward whichever candidate consumes the largest share of the
            // available budget without exceeding it -- NOT the cheapest.
            // budget/cost would saturate at 1 for every affordable candidate
            // and lose all signal; cost/budget keeps differentiating them.
            cu.expendatureRateUtility = (moneyBudget > 0f)
                ? Mathf.Clamp01(character._spawnCost / moneyBudget)
                : 0f;
            cu.expendatureRateisOverMax = character._spawnCost > moneyBudget;

            cu.expectedOutcomeRaw = UnitTracker.Instance.CalculateOverallExpectedOutcome(character, AIWeighted: true, relaventZone: aiSpawnZone);

            characterUtilities.Add(cu);
        }

        float maxAbsDelta = 0.0001f;
        foreach (var cu in characterUtilities)
        {
            cu.expectedOutcomeRaw -= totalExpectedOutcome;
            maxAbsDelta = Mathf.Max(maxAbsDelta, Mathf.Abs(cu.expectedOutcomeRaw));
        }

        foreach (var cu in characterUtilities)
        {
            cu.expectedOutcomeUtility = -cu.expectedOutcomeRaw / maxAbsDelta;
            cu.totalUtility = (cu.moneyUtility + cu.spawnCooldownUtility + cu.expendatureRateUtility + cu.expectedOutcomeUtility) / 4;
            if (showDebugLogs) cu.Print(moneyBudget);
        }

        CharacterUtility best = characterUtilities.OrderByDescending(cu => cu.totalUtility).First();
        if (showDebugLogs) Debug.Log($"highestCharacterUtility: {best.totalUtility} ({best.character.name})");

        if (!best.expendatureRateisOverMax)
        {
            GameObject spawned = SpawnObjects.EnemyInstance.Spawn(best.character);
            if (spawned != null)
            {
                characterCooldownHandler.StartCooldown(best.character);
                moneyBudget -= best.character._spawnCost;
                if (moneyBudget < 0f) moneyBudget = 0f;
            }
            if (showDebugLogs) Debug.Log($"Spawn Character wins: {best.character.name}. moneyBudget remaining: {moneyBudget}");
        }
        else
        {
            Money.AIInstance.UpgradeMaxMoney();
            if (showDebugLogs) Debug.Log($"Spawn Character wins but is over budget so not spawning: {best.character.name}. So trying to upgrade money. moneyBudget carries over: {moneyBudget}");
        }
    }
}

public class CharacterCooldownHandler
{
    public Dictionary<ScriptableStats, float> nextSpawnableTime = new Dictionary<ScriptableStats, float>();

    public void StartCooldown(ScriptableStats character)
    {
        nextSpawnableTime[character] = character._spawnCooldown + Time.time;
    }

    public float CooldownRemaining(ScriptableStats character)
    {
        if (!nextSpawnableTime.TryGetValue(character, out float readyTime)) return 0f;
        return Mathf.Max(0f, readyTime - Time.time);
    }
}

public class CharacterUtility
{
    public ScriptableStats character;
    public float moneyUtility;
    public float spawnCooldownUtility;
    public float expendatureRateUtility;
    public bool expendatureRateisOverMax = false;
    public float expectedOutcomeRaw;
    public float expectedOutcomeUtility;
    public float totalUtility;

    public void Print(float moneyBudget)
    {
        Debug.Log($"====== Utilities for {character.name} ======");
        /*Debug.Log($"moneyUtility: {moneyUtility} (0 = can't afford, 1 = fully affordable)");
        Debug.Log($"spawnCooldownUtility: {spawnCooldownUtility} (0 = just spawned, 1 = ready)");
        Debug.Log($"expendatureRateUtility: {expendatureRateUtility}. cost {character._spawnCost} vs moneyBudget {moneyBudget}. Actual Money: {Money.AIInstance.CurrentMoney}");
        Debug.Log($"expendatureRateisOverMax: {expendatureRateisOverMax}");
        Debug.Log($"expectedOutcomeUtility: {expectedOutcomeUtility} (raw delta: {expectedOutcomeRaw})"); */
        Debug.Log($"totalUtility: moneyUtility + spawnCooldownUtility + expendatureRateUtility + expectedOutcomeUtility (expectedOutcomeRaw) = totalUtility");
        Debug.Log($"totalUtility: {moneyUtility} + {spawnCooldownUtility} + {expendatureRateUtility} + {expectedOutcomeUtility} ({expectedOutcomeRaw})= {totalUtility}");
    }
}