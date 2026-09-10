using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Scriptable Object containing all AI behavior for a clan
/// Can be swapped per level to change AI difficulty/behavior
/// </summary>
[CreateAssetMenu(fileName = "New AI Clan", menuName = "AI/Clan Configuration")]
public class AIClanSO : ScriptableObject
{
    [Header("Clan Info")]
    public string clanName = "";
    [TextArea(3, 5)]
    public string description = "";
    
    [Header("AI Personalities")]
    public AIPersonality[] moods;
    public int startingMoodIndex = 0;

    [Header("Utility Normalization")]
    [Tooltip("Max unit count for normalization")]
    public float maxUnits = 20f;

    [Tooltip("Max enemy power for normalization")]
    public float maxEnemyPower = 50f;

    [Tooltip("Fallback max distance if bases aren't assigned")]
    public float fallbackMaxDistance = 100f;

    public float MaxDistance => UnitTracker.Instance != null ? UnitTracker.Instance.GetBaseDistance(fallbackMaxDistance) : fallbackMaxDistance;
}
/// <summary>
/// This is just a list if different decision loops
/// </summary>
[System.Serializable]
public class AIPersonality
{
    [Header("Mood Info")]
    public string moodName = "Mood";
    
    [TextArea(3, 5)]
    public string description = "Description of this mood's behavior";
    
    [Header("Decision Loops")]
    [Tooltip("Each loop operates independently with its own timing")]
    public List<AILoop> decisionLoops = new List<AILoop>();
}

/// <summary>
/// Timer logic, for when actions should be taken 
/// </summary>
[System.Serializable]
public class AILoop
{
    [Header("Loop Info")]
    [Tooltip("Disable to skip this loop entirely")]
    [SerializeField] public bool enabled = true;
    public string loopName = "New Loop";
    
    [Header("Timing")]
    [Tooltip("Base interval in seconds between decisions")]
    public float baseInterval = 3f;
    
    [Tooltip("Random variance (+/- seconds) to add unpredictability")]
    public float intervalVariance = 0.5f;
    
    [Header("Available Actions")]
    [Tooltip("All possible actions this loop can choose from")]
    [SerializeReference, SubclassSelector]
    public List<AIAction> possibleActions = new List<AIAction>();
    
    [Header("Debug")]
    [Tooltip("Toggle debug logs for this specific loop")]
    [SerializeField] public bool showDebugLogs = false;    
    [System.NonSerialized] public float currentTimer = 0f;
    [System.NonSerialized] public float currentInterval = 0f;
    [System.NonSerialized] public bool isExecutingSequence = false;

    public void Initialize()
    {
        ResetTimer();
    }
    
    public void ResetTimer()
    {
        currentInterval = baseInterval + Random.Range(-intervalVariance, intervalVariance);
        currentTimer = 0f;
    }
    public IEnumerator ExecuteLoop(AIClanSO clanConfig)
    {
        if (showDebugLogs) Debug.Log($"\n=== LOOP: {loopName} ===");

        AIAction bestAction = null;
        float bestUtility = -1f;

        foreach (var action in possibleActions)
        {
            if (action == null)
            {
                Debug.LogWarning($"[AI] Null action in loop {loopName}");
                continue;
            }

            if (!action.CanExecute(clanConfig))
            {
                if (showDebugLogs) Debug.Log($"  ✗ {action.actionName}: Cannot execute");
                continue;
            }

            float utility = action.CalculateUtility(clanConfig);

            if (showDebugLogs)
            {
                if (action.rootConsideration != null) Debug.Log($"  • {action.actionName}: {action.rootConsideration.GetDebugString(clanConfig)}");
                else Debug.Log($"  • {action.actionName}: {utility:F2}");
            }

            if (utility > bestUtility)
            {
                bestUtility = utility;
                bestAction = action;
            }
        }

        if (bestAction != null && bestUtility > 0f)
        {
            if (showDebugLogs) Debug.Log($"→ CHOSEN: {bestAction.actionName} (Utility: {bestUtility:F2})");
            yield return bestAction.Execute(clanConfig, this);
        }
        else
        {
            if (showDebugLogs) Debug.Log($"→ NO VALID ACTION (best utility: {bestUtility:F2})");
        }
    }

    public void AddDelay(float additionalTime)
    {
        currentInterval += additionalTime;
    }
    
    public bool UpdateTimer(float deltaTime)
    {
        if (isExecutingSequence) return false;
            
        currentTimer += deltaTime;
        return currentTimer >= currentInterval;
    }
}