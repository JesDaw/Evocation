using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AISpawnerController : MonoBehaviour
{
    public AIClanSO aiClan;
    public static AISpawnerController Instance { get; private set; }
    bool isRunning = false;
    int currentMoodIndex = 0;
    List<AILoop> currentLoops;

    #region setup
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (aiClan.moods != null && aiClan.moods.Length > 0)
        {
            currentMoodIndex = aiClan.startingMoodIndex;
            SetCurrentLoops();
        }
        ValidateSetup();
    }

    void SetCurrentLoops()
    {
        if (aiClan == null || aiClan.moods == null || aiClan.moods.Length == 0)
        {
            currentLoops = null;
            return;
        }

        currentMoodIndex = Mathf.Clamp(currentMoodIndex, 0, aiClan.moods.Length - 1);
        var mood = aiClan.moods[currentMoodIndex];
        currentLoops = mood.decisionLoops;
    }

    void ValidateSetup()
    {
        if (aiClan == null)
        {
            Debug.LogError("[AI] No AIClanScriptable assigned!");
            return;
        }

        if (currentLoops == null || currentLoops.Count == 0)
        {
            Debug.LogError($"[AI] {aiClan.clanName} has no decision loops!");
            return;
        }

        if (SpawnObjects.EnemyInstance == null) Debug.LogError("[AI] No enemy spawner found!");
    }

    #endregion

    public void StartAI()
    {
        if (isRunning)
        {
            Debug.LogWarning("[AI] Already running!");
            return;
        }

        if (aiClan == null)
        {
            Debug.LogError("[AI] Cannot start: No AI clan assigned!");
            return;
        }

        isRunning = true;

        foreach (var loop in currentLoops) loop.Initialize();

        StartCoroutine(UpdateAllLoops());
    }

    IEnumerator UpdateAllLoops()
    {
        while (isRunning)
        {
            foreach (var loop in currentLoops)
            {
                if (!loop.enabled) continue;

                if (loop.UpdateTimer(Time.deltaTime))
                {
                    StartCoroutine(loop.ExecuteLoop(aiClan));
                    loop.ResetTimer();
                }
            }
            yield return null;
        }
    }

    public AIPersonality GetCurrentMood()
    {
        if (aiClan == null || aiClan.moods == null || aiClan.moods.Length == 0) return null;
        return aiClan.moods[currentMoodIndex];
    }

    public void SetMoodByName(string moodName)
    {
        if (aiClan == null || aiClan.moods == null)
            return;

        for (int i = 0; i < aiClan.moods.Length; i++)
        {
            if (aiClan.moods[i].moodName == moodName)
            {
                currentMoodIndex = i;
                SetCurrentLoops();

                if (isRunning)
                {
                    foreach (var loop in currentLoops)
                        loop.Initialize();
                }
                return; // was missing: without this it kept looping and always logged "not found" after a match
            }
        }
        Debug.LogWarning($"[AI] Mood '{moodName}' not found!");
    }

    public void AddDelayToLoop(string loopName, float additionalTime)
    {
        var loop = currentLoops.FirstOrDefault(l => l.loopName == loopName);
        if (loop != null)
        {
            loop.AddDelay(additionalTime);
            if (loop.showDebugLogs) Debug.Log($"[AI] Added {additionalTime}s delay to loop '{loopName}'");
        }
        else
        {
            Debug.LogWarning($"[AI] Loop '{loopName}' not found!");
        }
    }

    public void AddDelayToAllLoops(float additionalTime)
    {
        foreach (var loop in currentLoops)
        {
            loop.AddDelay(additionalTime);
        }
    }

    public void StopAI()
    {
        isRunning = false;
        StopAllCoroutines();
    }

    #region Debug Methods

    [ContextMenu("Print Context State")]
    public void PrintContextState()
    {
        Debug.Log("\n=== AI CONTEXT STATE ===");

        if (Timer.Instance != null)
        {
            Debug.Log($"Time Elapsed: {Timer.Instance.ElapsedTimeSeconds:F1}s (Norm: {Timer.Instance.GetNormalizedElapsed():F2})");
            Debug.Log($"Time Remaining: {Timer.Instance.RemainingTimeSeconds:F1}s (Norm: {Timer.Instance.GetNormalizedRemaining():F2})");
        }

        if (UnitTracker.Instance != null)
        {
            Debug.Log($"Player Units: {UnitTracker.Instance.GetTeamUnitCount("Player")}");
            Debug.Log($"Enemy Units: {UnitTracker.Instance.GetTeamUnitCount("Enemy")}");

            if (UnitTracker.Instance.EnemyBase != null)
            {
                float closest = UnitTracker.Instance.GetClosestPlayerUnitDistance(UnitTracker.Instance.EnemyBase.position, out float power);
                Debug.Log($"Closest Enemy: {closest:F1}m, Power: {power:F1}");
            }
        }
        Debug.Log("====================\n");
    }

    [ContextMenu("Print Loop States")]
    public void PrintLoopStates()
    {
        if (currentLoops == null || currentLoops.Count == 0)
        {
            Debug.Log("[AI] No loops to display");
            return;
        }

        Debug.Log("\n=== LOOP STATES ===");
        foreach (var loop in currentLoops)
        {
            Debug.Log($"[{Time.time:F3}s] {loop.loopName}:");
            Debug.Log($"  Enabled: {loop.enabled}");
            Debug.Log($"  Timer: {loop.currentTimer:F2}/{loop.currentInterval:F2}s");
            Debug.Log($"  Executing Sequence: {loop.isExecutingSequence}");
            Debug.Log($"  Available Actions: {loop.possibleActions.Count}");
        }
        Debug.Log("==================\n");
    }

    #endregion
}