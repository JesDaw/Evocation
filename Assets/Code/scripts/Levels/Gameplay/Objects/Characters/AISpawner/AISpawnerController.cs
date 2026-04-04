using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// Main AI Controller - Clean version with comprehensive debug logging
/// </summary>
public class AISpawnerController : MonoBehaviour
{
    [Header("AI Moods")]
    public List<AIMood> moods = new List<AIMood>();
    [SerializeField] int startingMoodIndex = 0;
    
    [Header("Decision Making")]
    [SerializeField] float decisionInterval = 2f;
    
    [Header("Spawner Setup")]
    [SerializeField] AIMoneyManager aiMoneyManager;
    
    [Header("Game Systems")]
    [SerializeField] Timer gameTimer;
    
    [Header("Map Zones")]
    [SerializeField] MapZonesManager upperZone;
    [SerializeField] MapZonesManager middleZone;
    [SerializeField] MapZonesManager lowerZone;
    
    [Header("Spatial")]
    [SerializeField] Transform aiBase;
    [SerializeField] Transform playerBase;
    // maxDistance is now calculated automatically from base positions
    
    [Header("Normalization Settings")]
    [Tooltip("Max money for normalization (0-1)")]
    [SerializeField] float maxMoney = 100f;
    [Tooltip("Max unit count for normalization")]
    [SerializeField] float maxUnits = 20f;
    [Tooltip("Max enemy power for normalization")]
    [SerializeField] float maxEnemyPower = 50f;
    [Tooltip("Time in seconds before TimeSinceLastAction maxes out")]
    [SerializeField] float maxActionWaitTime = 15f;
    
    [Header("Debug")]
    [SerializeField] bool showDebugLogs = false;

    private AIMood currentMood;
    private int currentMoodIndex;
    private AIContext context;
    private bool isRunning = false;

    void Start()
    {
        ValidateSetup();
        InitializeMood();
        InitializeContext();
    }

    void ValidateSetup()
    {
        if (moods.Count == 0)
        {
            Debug.LogError("[AI] No moods configured! Add at least one mood in the inspector.");
            return;
        }

        if (SpawnObjects.EnemyInstance == null)
            Debug.LogError("[AI] No SpawnObjects assigned!");

        if (aiMoneyManager == null)
            Debug.LogError("[AI] No AIMoneyManager assigned!");
    }

    void InitializeMood()
    {
        currentMoodIndex = Mathf.Clamp(startingMoodIndex, 0, moods.Count - 1);
        currentMood = moods[currentMoodIndex];
        
        if (showDebugLogs)
            Debug.Log($"[AI] Initialized with mood: {currentMood.moodName}");
    }

    void InitializeContext()
    {
        float calculatedMaxDistance = (aiBase != null && playerBase != null) 
            ? Vector3.Distance(aiBase.position, playerBase.position) 
            : 100f;

        context = new AIContext
        {
            spawner = SpawnObjects.EnemyInstance,
            aiMoneyManager = aiMoneyManager,
            timer = gameTimer,
            upperZone = upperZone,
            middleZone = middleZone,
            lowerZone = lowerZone,
            aiBase = aiBase,
            playerBase = playerBase,
            maxDistance = calculatedMaxDistance,
            maxMoney = maxMoney,
            maxUnits = maxUnits,
            maxEnemyPower = maxEnemyPower,
            maxActionWaitTime = maxActionWaitTime,
            showDebugLogs = showDebugLogs,
            lastActionTime = Time.time
        };
    }

    public void StartAI()
    {
        if (isRunning)
        {
            Debug.LogWarning("[AI] Already running!");
            return;
        }
        if (SpawnObjects.EnemyInstance == null)
        {
            Debug.LogError("[AI] Cannot start: Missing spawner");
        }

        if (aiMoneyManager == null)
        {
            Debug.LogError("[AI] Cannot start: Missing AIMoneyManager!");
            return;
        }

        isRunning = true;
        StartCoroutine(AIDecisionLoop());
        
        if (showDebugLogs)
            Debug.Log($"[AI] Started in {currentMood.moodName} mood");
    }

    public void StopAI()
    {
        isRunning = false;
        StopAllCoroutines();
        
        if (showDebugLogs)
            Debug.Log("[AI] Stopped");
    }

    IEnumerator AIDecisionLoop()
    {
        while (isRunning)
        {
            context.UpdateContext();

            List<AIAction> availableActions = currentMood.availableActions;

            if (availableActions.Count == 0)
            {
                Debug.LogWarning($"[AI] Mood '{currentMood.moodName}' has no actions!");
                yield return new WaitForSeconds(decisionInterval);
                continue;
            }

            // CRITICAL: Track best action
            // With multiplication, utility is always between 0 and 1
            // 0 means absolute veto (action can't be taken)
            AIAction bestAction = null;
            float bestUtility = 0f;

            if (showDebugLogs)
            {
                Debug.Log($"\n{'='}{'='} AI DECISION {'='}{' '}");
                Debug.Log($"Mood: {currentMood.moodName}");
                Debug.Log($"Money: {context.GetCurrentMoney():F1}");
                //Debug.Log($"AI Units: {context.GetAIUnitCount()}\n");
            }

            // Evaluate each action
            foreach (AIAction action in availableActions)
            {
                if (action == null)
                {
                    Debug.LogWarning("[AI] Null action!");
                    continue;
                }

                // Calculate utility with detailed logging
                float utility = action.CalculateUtility(context, showDebugLogs);

                // Track best action
                if (utility > bestUtility)
                {
                    bestUtility = utility;
                    bestAction = action;
                }
            }

            // Execute best action only if utility >= 0.1
            if (bestAction != null && bestUtility >= 0.1f)
            {
                if (showDebugLogs)
                    Debug.Log($"\n→ CHOSEN: {bestAction.actionName} (Utility: {bestUtility:F2})\n");
                
                bestAction.Execute(context);
                context.lastActionTime = Time.time;
                context.currentLoopCount++;
            }
            else
            {
                // Fallback: if no action could execute, try DoNothingAction
                DoNothingAction doNothing = availableActions.OfType<DoNothingAction>().FirstOrDefault();
                if (doNothing != null)
                {
                    if (showDebugLogs)
                        Debug.Log($"\n→ FALLBACK: No valid actions, doing nothing\n");
                    
                    doNothing.Execute(context);
                    context.currentLoopCount++;
                }
                else if (showDebugLogs)
                {
                    Debug.Log($"\n→ NO ACTIONS AVAILABLE (no DoNothing fallback found)\n");
                }
            }

            yield return new WaitForSeconds(decisionInterval);
        }
    }

    #region Mood Control
    
    public void SetMood(int moodIndex)
    {
        if (moodIndex < 0 || moodIndex >= moods.Count)
        {
            Debug.LogError($"[AI] Invalid mood index: {moodIndex}");
            return;
        }

        AIMood oldMood = currentMood;
        currentMoodIndex = moodIndex;
        currentMood = moods[currentMoodIndex];
        
        if (showDebugLogs)
            Debug.Log($"[AI] Mood changed: {oldMood?.moodName} → {currentMood.moodName}");
    }
    
    public void SetMoodByName(string moodName)
    {
        for (int i = 0; i < moods.Count; i++)
        {
            if (moods[i].moodName == moodName)
            {
                SetMood(i);
                return;
            }
        }
        
        Debug.LogError($"[AI] Mood '{moodName}' not found!");
    }
    
    #endregion

    #region Debug Menu
    
    [ContextMenu("Print Context State")]
    public void PrintContextState()
    {
        if (context == null)
        {
            Debug.Log("[AI] Context not initialized");
            return;
        }
        
        context.UpdateContext();
        
        Debug.Log("\n=== AI CONTEXT STATE ===");
        Debug.Log($"Money: {context.GetCurrentMoney():F1} (Normalized: {context.GetNormalizedMoney():F2})");
        Debug.Log($"Time Elapsed: {context.GetTimeElapsed():F1}s (Norm: {context.GetNormalizedTimeElapsed():F2})");
        Debug.Log($"Time Remaining: {context.GetTimeRemaining():F1}s (Norm: {context.GetNormalizedTimeRemaining():F2})");
        Debug.Log($"Closest Enemy: {context.GetClosestEnemyDistance():F1} (Norm: {context.GetNormalizedClosestEnemy():F2})");
        Debug.Log($"Player Units: {context.GetPlayerUnitCount()} (Norm: {context.GetNormalizedPlayerUnits():F2})");
        Debug.Log($"AI Units: {context.GetAIUnitCount()} (Norm: {context.GetNormalizedAIUnits():F2})");
        Debug.Log("===================\n");
    }
    
    [ContextMenu("Debug Unit Counting")]
    public void DebugUnitCounting()
    {
        GameObject[] enemyObjs = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log($"\n=== UNIT COUNT DEBUG ===");
        Debug.Log($"Total 'Enemy' tagged objects: {enemyObjs.Length}");
        
        Dictionary<string, int> rootCounts = new Dictionary<string, int>();
        
        foreach (GameObject obj in enemyObjs)
        {
            Transform root = obj.transform;
            while (root.parent != null)
                root = root.parent;
            
            string rootName = root.name;
            if (!rootCounts.ContainsKey(rootName))
                rootCounts[rootName] = 0;
            rootCounts[rootName]++;
            
            string parentInfo = obj.transform.parent != null ? $" (child of {root.name})" : " (root)";
            Debug.Log($"  {obj.name}{parentInfo}");
        }
        
        Debug.Log($"\nRoot objects: {rootCounts.Count}");
        foreach (var kvp in rootCounts)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} tagged object(s)");
        }
        Debug.Log("====================\n");
    }
    
    #endregion
}