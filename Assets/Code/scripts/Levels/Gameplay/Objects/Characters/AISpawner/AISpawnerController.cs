using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Main AI Controller - The Brain
/// FIXED: Better debugging for unit counts and utility calculations
/// </summary>
public class AISpawnerController : MonoBehaviour
{
    [Header("AI Moods")]
    [Tooltip("All moods for this AI - edit directly in inspector!")]
    public List<AIMood> moods = new List<AIMood>();
    
    [Tooltip("Starting mood index (0 = first mood in list)")]
    [SerializeField] private int startingMoodIndex = 0;
    
    [Header("Decision Making")]
    [SerializeField] private float decisionInterval = 2f;
    
    [Header("Spawner Setup - REQUIRED")]
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
    [SerializeField] private float maxDistance = 50f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showUnitCountDetails = true;

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
            Debug.LogError("AISpawnerController has no moods! Add at least one mood in the inspector.");
            return;
        }

        if (spawner == null)
            Debug.LogError("AISpawnerController: No SpawnObjects assigned!");

        if (aiMoneyManager == null)
            Debug.LogError("AISpawnerController: No AIMoneyManager assigned!");

        if (spawner != null && showDebugLogs)
            Debug.Log("Make sure SpawnObjects has 'enemySpawner' checked!");
    }

    void InitializeMood()
    {
        currentMoodIndex = Mathf.Clamp(startingMoodIndex, 0, moods.Count - 1);
        currentMood = moods[currentMoodIndex];
        
        if (showDebugLogs)
            Debug.Log($"AI initialized with mood: {currentMood.moodName}");
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
            maxDistance = maxDistance
        };
    }

    public void StartAI()
    {
        if (isRunning)
        {
            Debug.LogWarning("AI is already running!");
            return;
        }

        if (spawner == null || aiMoneyManager == null)
        {
            Debug.LogError("Cannot start AI: Missing spawner or AIMoneyManager!");
            return;
        }

        isRunning = true;
        StartCoroutine(AIDecisionLoop());
        
        if (showDebugLogs)
            Debug.Log($"AI Started in {currentMood.moodName} mood");
    }

    public void StopAI()
    {
        isRunning = false;
        StopAllCoroutines();
        
        if (showDebugLogs)
            Debug.Log("AI Stopped");
    }

    IEnumerator AIDecisionLoop()
    {
        while (isRunning)
        {
            context.UpdateContext();

            List<AIActionWrapper> availableActions = currentMood.availableActions;

            if (availableActions.Count == 0)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"AI mood '{currentMood.moodName}' has no actions!");
                yield return new WaitForSeconds(decisionInterval);
                continue;
            }

            // Find best action
            AIActionWrapper bestActionWrapper = null;
            AIAction bestAction = null;
            float bestUtility = float.MinValue;

            if (showDebugLogs)
            {
                Debug.Log($"\n=== AI Decision ({currentMood.moodName} mood) ===");
                Debug.Log($"Current Money: {context.GetCurrentMoney():F1}");
                Debug.Log($"AI Units: {context.GetAIUnitCount()}");
                if (showUnitCountDetails)
                {
                    DebugUnitCounts();
                }
            }

            foreach (AIActionWrapper wrapper in availableActions)
            {
                if (wrapper == null)
                {
                    Debug.LogWarning("  Null action wrapper in mood!");
                    continue;
                }
                
                AIAction action = wrapper.GetAction();
                if (action == null)
                {
                    Debug.LogWarning($"  Action is null for type: {wrapper.actionType}");
                    continue;
                }
                
                if (showDebugLogs)
                    Debug.Log($"\n  Evaluating: {action.actionName}");
                
                // Check if can execute
                bool canExecute = action.CanExecute(context);
                if (showDebugLogs)
                    Debug.Log($"    Can Execute: {canExecute}");
                
                // Calculate utility
                float baseUtility = action.CalculateUtility(context);
                
                if (showDebugLogs)
                    Debug.Log($"    Base utility: {baseUtility:F2}");
                
                // Apply mood modifiers
                float finalUtility = currentMood.ModifyUtility(baseUtility, wrapper);
                
                // DEBUG: Show modifier breakdown
                if (showDebugLogs && baseUtility > float.MinValue)
                {
                    Debug.Log($"    Mood Modifiers:");
                    Debug.Log($"      Global Bonus: {currentMood.globalUtilityBonus}");
                    Debug.Log($"      Global Multiplier: {currentMood.globalUtilityMultiplier}");
                    Debug.Log($"      Action Bonus: {wrapper.bonusUtility}");
                    Debug.Log($"      Action Multiplier: {wrapper.utilityMultiplier}");
                    Debug.Log($"    Final utility: {finalUtility:F2}");
                }

                if (finalUtility > bestUtility)
                {
                    bestUtility = finalUtility;
                    bestAction = action;
                    bestActionWrapper = wrapper;
                }
            }

            // Execute best action
            if (bestAction != null && bestUtility > float.MinValue)
            {
                if (showDebugLogs)
                    Debug.Log($"\n→ AI chose: {bestAction.actionName} (Utility: {bestUtility:F2})");
                
                bestAction.Execute(context);
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("\n→ AI found no valid actions");
            }

            yield return new WaitForSeconds(decisionInterval);
        }
    }

    /// <summary>
    /// Debug what's being counted as AI units
    /// </summary>
    private void DebugUnitCounts()
    {
        GameObject[] aiUnits = GameObject.FindGameObjectsWithTag("Enemy");
        
        Debug.Log($"  Found {aiUnits.Length} GameObjects with 'Enemy' tag:");
        foreach (GameObject obj in aiUnits)
        {
            string parentInfo = obj.transform.parent != null ? $" (parent: {obj.transform.parent.name})" : " (root object)";
            Debug.Log($"    - {obj.name}{parentInfo}");
        }
    }

    #region Mood Control
    
    public void SetMood(int moodIndex)
    {
        if (moodIndex < 0 || moodIndex >= moods.Count)
        {
            Debug.LogError($"Invalid mood index: {moodIndex}");
            return;
        }

        AIMood oldMood = currentMood;
        currentMoodIndex = moodIndex;
        currentMood = moods[currentMoodIndex];
        
        if (showDebugLogs)
            Debug.Log($"AI mood changed: {oldMood?.moodName} → {currentMood.moodName}");
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
        
        Debug.LogError($"Mood '{moodName}' not found!");
    }
    
    public void NextMood()
    {
        int nextIndex = (currentMoodIndex + 1) % moods.Count;
        SetMood(nextIndex);
    }
    
    public void PreviousMood()
    {
        int prevIndex = (currentMoodIndex - 1 + moods.Count) % moods.Count;
        SetMood(prevIndex);
    }
    
    public string GetCurrentMoodName()
    {
        return currentMood != null ? currentMood.moodName : "None";
    }
    
    #endregion

    #region Debug/Testing
    
    [ContextMenu("Force AI Decision")]
    public void ForceDecision()
    {
        if (context == null) InitializeContext();
        
        context.UpdateContext();

        AIActionWrapper bestWrapper = null;
        AIAction bestAction = null;
        float bestUtility = float.MinValue;

        foreach (AIActionWrapper wrapper in currentMood.availableActions)
        {
            if (wrapper == null) continue;
            
            AIAction action = wrapper.GetAction();
            if (action == null) continue;
            
            float utility = action.CalculateUtility(context);
            utility = currentMood.ModifyUtility(utility, wrapper);
            
            if (utility > bestUtility)
            {
                bestUtility = utility;
                bestAction = action;
                bestWrapper = wrapper;
            }
        }

        if (bestAction != null)
        {
            Debug.Log($"Forced decision: {bestAction.actionName} (Utility: {bestUtility:F2})");
            bestAction.Execute(context);
        }
    }
    
    [ContextMenu("Print Context State")]
    public void PrintContextState()
    {
        if (context == null)
        {
            Debug.Log("Context not initialized");
            return;
        }
        
        context.UpdateContext();
        
        Debug.Log("=== AI Context State ===");
        Debug.Log($"Money: {context.GetCurrentMoney():F1} (Normalized: {context.GetNormalizedMoney():F2})");
        Debug.Log($"Time Elapsed: {context.GetTimeElapsed():F1}s");
        Debug.Log($"Time Remaining: {context.GetTimeRemaining():F1}s");
        Debug.Log($"Closest Enemy: {context.GetClosestEnemyDistance():F1}");
        Debug.Log($"Player Units: {context.GetPlayerUnitCount()}");
        Debug.Log($"AI Units: {context.GetAIUnitCount()}");
    }
    
    [ContextMenu("Debug Unit Counting")]
    public void DebugUnitCountingFull()
    {
        Debug.Log("\n=== UNIT COUNT DEBUG ===");
        
        GameObject[] enemyObjs = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log($"\nTotal 'Enemy' tagged objects: {enemyObjs.Length}");
        
        Dictionary<string, int> parentCounts = new Dictionary<string, int>();
        
        foreach (GameObject obj in enemyObjs)
        {
            Transform rootParent = obj.transform;
            while (rootParent.parent != null)
                rootParent = rootParent.parent;
            
            string rootName = rootParent.name;
            if (!parentCounts.ContainsKey(rootName))
                parentCounts[rootName] = 0;
            parentCounts[rootName]++;
            
            Debug.Log($"  {obj.name} → Root: {rootName}");
        }
        
        Debug.Log($"\nGrouped by root parent:");
        foreach (var kvp in parentCounts)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} tagged objects");
        }
        
        Debug.Log($"\nActual spawned units (root level): {parentCounts.Count}");
        Debug.Log("=== END DEBUG ===\n");
    }
    
    [ContextMenu("Debug All Considerations")]
    public void DebugAllConsiderations()
    {
        if (context == null)
        {
            Debug.LogError("Context not initialized! Start the game first.");
            return;
        }
        
        context.UpdateContext();
        
        Debug.Log("=== Debugging All Considerations ===");
        
        if (currentMood == null)
        {
            Debug.LogError("No current mood!");
            return;
        }
        
        Debug.Log($"Current Mood: {currentMood.moodName}");
        Debug.Log($"Actions in mood: {currentMood.availableActions.Count}");
        
        foreach (AIActionWrapper wrapper in currentMood.availableActions)
        {
            if (wrapper == null)
            {
                Debug.LogWarning("  Null wrapper!");
                continue;
            }
            
            AIAction action = wrapper.GetAction();
            if (action == null)
            {
                Debug.LogWarning($"  Null action for type: {wrapper.actionType}");
                continue;
            }
            
            Debug.Log($"\n  Action: {action.actionName}");
            Debug.Log($"    Can Execute: {action.CanExecute(context)}");
            Debug.Log($"    Considerations: {action.considerations.Count}");
            
            if (action.considerations.Count == 0)
            {
                Debug.LogWarning($"    WARNING: No considerations! This action will always return 0 utility.");
            }
            
            foreach (AIConsideration consideration in action.considerations)
            {
                if (consideration == null)
                {
                    Debug.LogWarning("      Null consideration!");
                    continue;
                }
                
                Debug.Log($"      {consideration.considerationName}:");
                Debug.Log($"        Type: {consideration.type}");
                Debug.Log($"        Weight: {consideration.weight}");
                
                float normalized = 0f;
                switch (consideration.type)
                {
                    case ConsiderationType.Money:
                        normalized = context.GetNormalizedMoney();
                        break;
                    case ConsiderationType.TimeElapsed:
                        normalized = context.GetNormalizedTimeElapsed();
                        break;
                    case ConsiderationType.TimeRemaining:
                        normalized = context.GetNormalizedTimeRemaining();
                        break;
                    case ConsiderationType.ClosestEnemyDistance:
                        normalized = context.GetNormalizedClosestEnemy();
                        break;
                    case ConsiderationType.PlayerUnitCount:
                        normalized = context.GetNormalizedPlayerUnits();
                        break;
                    case ConsiderationType.AIUnitCount:
                        normalized = context.GetNormalizedAIUnits();
                        break;
                }
                
                float curveValue = consideration.responseCurve.Evaluate(normalized);
                float result = curveValue * consideration.weight;
                
                Debug.Log($"        Normalized: {normalized:F2}");
                Debug.Log($"        Curve Output: {curveValue:F2}");
                Debug.Log($"        Final Value: {result:F2}");
            }
        }
        
        Debug.Log("\n=== End Debug ===");
    }
    
    #endregion

    void OnDrawGizmosSelected()
    {
        if (aiBase != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(aiBase.position, maxDistance * 0.3f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(aiBase.position, maxDistance);
        }

        if (upperZone != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, upperZone.transform.position);
        }
        if (middleZone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, middleZone.transform.position);
        }
        if (lowerZone != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, lowerZone.transform.position);
        }
        if (spawner != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, spawner.transform.position);
        }
    }
}