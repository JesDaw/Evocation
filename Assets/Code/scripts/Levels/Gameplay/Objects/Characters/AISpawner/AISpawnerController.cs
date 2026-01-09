using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Main AI Controller - Clean version with comprehensive debug logging
/// </summary>
public class AISpawnerController : MonoBehaviour
{
    [Header("AI Moods")]
    public List<AIMood> moods = new List<AIMood>();
    [SerializeField] private int startingMoodIndex = 0;
    
    [Header("Decision Making")]
    [SerializeField] private float decisionInterval = 2f;
    
    [Header("Spawner Setup")]
    [SerializeField] private SpawnObjects spawner;
    [SerializeField] private AIMoneyManager aiMoneyManager;
    
    [Header("Game Systems")]
    [SerializeField] private Timer gameTimer;
    
    [Header("Map Zones")]
    [SerializeField] private MapZonesManager upperZone;
    [SerializeField] private MapZonesManager middleZone;
    [SerializeField] private MapZonesManager lowerZone;
    
    [Header("Spatial")]
    [SerializeField] private Transform aiBase;
    [SerializeField] private Transform playerBase;
    [SerializeField] private float maxDistance = 100f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

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

        if (spawner == null)
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
        context = new AIContext
        {
            spawner = spawner,
            aiMoneyManager = aiMoneyManager,
            timer = gameTimer,
            upperZone = upperZone,
            middleZone = middleZone,
            lowerZone = lowerZone,
            aiBase = aiBase,
            playerBase = playerBase,
            maxDistance = maxDistance,
            showDebugLogs = showDebugLogs
        };
    }

    public void StartAI()
    {
        if (isRunning)
        {
            Debug.LogWarning("[AI] Already running!");
            return;
        }

        if (spawner == null || aiMoneyManager == null)
        {
            Debug.LogError("[AI] Cannot start: Missing spawner or AIMoneyManager!");
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

            List<AIActionWrapper> availableActions = currentMood.availableActions;

            if (availableActions.Count == 0)
            {
                Debug.LogWarning($"[AI] Mood '{currentMood.moodName}' has no actions!");
                yield return new WaitForSeconds(decisionInterval);
                continue;
            }

            // CRITICAL: Track best action
            AIAction bestAction = null;
            float bestUtility = float.MinValue;

            if (showDebugLogs)
            {
                Debug.Log($"\n{'='}{'='} AI DECISION {'='}{' '}");
                Debug.Log($"Mood: {currentMood.moodName}");
                Debug.Log($"Money: {context.GetCurrentMoney():F1}");
                //Debug.Log($"AI Units: {context.GetAIUnitCount()}\n");
            }

            // Evaluate each action
            foreach (AIActionWrapper wrapper in availableActions)
            {
                if (wrapper == null)
                {
                    Debug.LogWarning("[AI] Null action wrapper!");
                    continue;
                }
                
                AIAction action = wrapper.GetAction();
                if (action == null)
                {
                    Debug.LogWarning($"[AI] Null action for type: {wrapper.actionType}");
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

            // Execute best action
            if (bestAction != null && bestUtility > float.MinValue)
            {
                if (showDebugLogs)
                    Debug.Log($"\n→ CHOSEN: {bestAction.actionName} (Utility: {bestUtility:F2})\n");
                
                bestAction.Execute(context);
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log($"\n→ NO VALID ACTIONS (Best utility: {bestUtility:F2})\n");
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