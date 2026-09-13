using UnityEngine;
using System.Collections;

public class AILevelHarness : MonoBehaviour
{
    public AIClan aiClan;
    public static AILevelHarness Instance { get; private set; }
    public float tickInterval = 0.75f;

    public bool showDebugLogs = false;

    bool isRunning = false;
    int currentPhaseIndex = 0;
    AISubState currentSubState;
    Coroutine AILoopCoroutine;

    public AIPhase CurrentPhase =>
        (aiClan != null && aiClan.Phases != null && aiClan.Phases.Length > 0)
            ? aiClan.Phases[Mathf.Clamp(currentPhaseIndex, 0, aiClan.Phases.Length - 1)]
            : null;

    public AISubState CurrentSubState => currentSubState;
    #region startup

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
    }
    #endregion
    public void StartAI()
    {
        if(showDebugLogs) Debug.Log($"Starting AI");
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
        currentSubState = CurrentPhase.EvaluateSubState(this, aiClan.SpawnableCharacters);
        while (isRunning)
        {
            currentSubState.ExecuteFlowchart(this);
            yield return new WaitForSeconds(tickInterval);
        }
    }

    public void IncramentPhase()
    {
        if (currentPhaseIndex >= aiClan.Phases.Length) return;
        currentPhaseIndex++;
        if (showDebugLogs) Debug.Log($"[AI] Phase advanced -> {CurrentPhase.GetType().Name}");
        currentSubState = CurrentPhase.EvaluateSubState(this, aiClan.SpawnableCharacters);
    }

    public void ChangePhaseByName(string name)
    {
        for (int i = 0; i < aiClan.Phases.Length; i++)
        {
            if (aiClan.Phases[i].GetType().Name == name)
            {
                currentPhaseIndex = i;
                currentSubState = CurrentPhase.EvaluateSubState(this, aiClan.SpawnableCharacters);
                break;
            }
        }
    }

    public void StopAI()
    {
        if(showDebugLogs) Debug.Log($"Stopping AI");
        isRunning = false;
        if (AILoopCoroutine != null) StopCoroutine(AILoopCoroutine);
    }
}
