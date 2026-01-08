using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Main AI Controller - The Brain
/// Handles mood changes and makes spawning decisions
/// Moods are now changed via public functions - trigger from events!
/// </summary>
public class AISpawnerController : MonoBehaviour
{
    [Header("AI Moods")]
    [Tooltip("All available moods for this AI - changes between phases")]
    [SerializeField] private List<AIPersonality> moods = new List<AIPersonality>();
    
    [Tooltip("Starting mood index (0 = first mood in list)")]
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
    [SerializeField] private float maxDistance = 50f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private AIPersonality currentMood;
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
            Debug.LogError("AISpawnerController has no moods assigned!");
            return;
        }

        if (spawner == null)
        {
            Debug.LogError("AISpawnerController: No SpawnObjects assigned!");
        }

        if (aiMoneyManager == null)
        {
            Debug.LogError("AISpawnerController: No Money manager assigned!");
        }

        // Verify spawner is set to enemy mode
        if (spawner != null)
        {
            // The spawner should have enemySpawner = true for AI
            // We can't check this directly since it's private, but we can warn
            if (showDebugLogs)
                Debug.Log("Make sure SpawnObjects has 'enemySpawner' checked for AI!");
        }
    }

    void InitializeMood()
    {
        currentMoodIndex = Mathf.Clamp(startingMoodIndex, 0, moods.Count - 1);
        currentMood = moods[currentMoodIndex];
        
        if (showDebugLogs)
            Debug.Log($"AI initialized with mood: {currentMood.moodName}");
    }

    /// <summary>
    /// Initialize the AI context with references to game systems
    /// </summary>
    void InitializeContext()
    {
        context = new AIContext
        {
            // Spawner and money (REQUIRED)
            spawner = spawner,
            aiMoneyManager = aiMoneyManager,
            
            // Game systems
            timer = gameTimer,
            
            // Map zones
            upperZone = upperZone,
            middleZone = middleZone,
            lowerZone = lowerZone,
            
            // Spatial
            aiBase = aiBase,
            playerBase = playerBase,
            maxDistance = maxDistance,
        };
    }

    /// <summary>
    /// Start the AI decision-making loop
    /// Call this when game starts (e.g., from timer activation)
    /// </summary>
    public void StartAI()
    {
        if (isRunning)
        {
            Debug.LogWarning("AI is already running!");
            return;
        }

        if (spawner == null || aiMoneyManager == null)
        {
            Debug.LogError("Cannot start AI: Missing required components (Spawner or Money)!");
            return;
        }

        isRunning = true;
        StartCoroutine(AIDecisionLoop());
        
        if (showDebugLogs)
            Debug.Log($"AI Started in {currentMood.moodName} mood");
    }

    /// <summary>
    /// Stop the AI
    /// </summary>
    public void StopAI()
    {
        isRunning = false;
        StopAllCoroutines();
        
        if (showDebugLogs)
            Debug.Log("AI Stopped");
    }

    /// <summary>
    /// Main AI decision loop
    /// </summary>
    IEnumerator AIDecisionLoop()
    {
        while (isRunning)
        {
            // Update context with current game state
            context.UpdateContext();

            // Get available actions from current mood
            List<AIAction> availableActions = currentMood.availableActions;

            if (availableActions.Count == 0)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"AI mood '{currentMood.moodName}' has no available actions!");
                yield return new WaitForSeconds(decisionInterval);
                continue;
            }

            // UTILITY-BASED DECISION MAKING
            AIAction bestAction = null;
            float bestUtility = float.MinValue;

            if (showDebugLogs)
                Debug.Log($"=== AI Decision ({currentMood.moodName} mood) ===");

            // Evaluate each action
            foreach (AIAction action in availableActions)
            {
                if (action == null) continue;
                
                // Calculate base utility from action's considerations
                float utility = action.CalculateUtility(context);
                
                // Apply mood modifiers
                utility = currentMood.ModifyUtility(utility, action);

                if (showDebugLogs)
                    Debug.Log($"  {action.actionName}: Utility = {utility:F2}");

                if (utility > bestUtility)
                {
                    bestUtility = utility;
                    bestAction = action;
                }
            }

            // Execute the best action
            if (bestAction != null && bestUtility > float.MinValue)
            {
                if (showDebugLogs)
                    Debug.Log($"→ AI chose: {bestAction.actionName} (Utility: {bestUtility:F2})");
                
                bestAction.Execute(context);
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("→ AI found no valid actions");
            }

            // Wait before next decision
            yield return new WaitForSeconds(decisionInterval);
        }
    }

    #region Mood Control (Call these from events!)
    
    /// <summary>
    /// Change to a specific mood by index
    /// </summary>
    public void SetMood(int moodIndex)
    {
        if (moodIndex < 0 || moodIndex >= moods.Count)
        {
            Debug.LogError($"Invalid mood index: {moodIndex}");
            return;
        }

        AIPersonality oldMood = currentMood;
        currentMoodIndex = moodIndex;
        currentMood = moods[currentMoodIndex];
        
        if (showDebugLogs)
            Debug.Log($"AI mood changed: {oldMood?.moodName} → {currentMood.moodName}");
    }
    
    /// <summary>
    /// Change to a specific mood by name
    /// </summary>
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
    
    /// <summary>
    /// Cycle to next mood in list
    /// </summary>
    public void NextMood()
    {
        int nextIndex = (currentMoodIndex + 1) % moods.Count;
        SetMood(nextIndex);
    }
    
    /// <summary>
    /// Cycle to previous mood in list
    /// </summary>
    public void PreviousMood()
    {
        int prevIndex = (currentMoodIndex - 1 + moods.Count) % moods.Count;
        SetMood(prevIndex);
    }
    
    /// <summary>
    /// Get current mood name (useful for UI/debugging)
    /// </summary>
    public string GetCurrentMoodName()
    {
        return currentMood != null ? currentMood.moodName : "None";
    }
    
    /// <summary>
    /// Get all mood names (useful for UI dropdowns)
    /// </summary>
    public List<string> GetAllMoodNames()
    {
        List<string> names = new List<string>();
        foreach (AIPersonality mood in moods)
        {
            names.Add(mood.moodName);
        }
        return names;
    }
    
    #endregion

    #region Public Getters for AI State
    
    /// <summary>
    /// Get current money amount
    /// </summary>
    public float GetCurrentMoney()
    {
        return aiMoneyManager != null ? aiMoneyManager.GetMoney() : 0f;
    }
    
    /// <summary>
    /// Get current unit count
    /// </summary>
    public int GetCurrentUnitCount()
    {
        return context != null ? context.GetAIUnitCount() : 0;
    }
    
    /// <summary>
    /// Check if AI can afford a unit
    /// </summary>
    public bool CanAfford(float cost)
    {
        return GetCurrentMoney() >= cost;
    }
    
    /// <summary>
    /// Check if spawning is currently enabled
    /// </summary>
    public bool IsSpawningEnabled()
    {
        return spawner != null && spawner.spawningEnabled;
    }
    
    #endregion

    #region Debug/Testing
    
    /// <summary>
    /// Manually trigger an AI decision (for testing)
    /// </summary>
    [ContextMenu("Force AI Decision")]
    public void ForceDecision()
    {
        if (context == null) InitializeContext();
        
        context.UpdateContext();

        AIAction bestAction = null;
        float bestUtility = float.MinValue;

        foreach (AIAction action in currentMood.availableActions)
        {
            if (action == null) continue;
            
            float utility = action.CalculateUtility(context);
            utility = currentMood.ModifyUtility(utility, action);
            
            if (utility > bestUtility)
            {
                bestUtility = utility;
                bestAction = action;
            }
        }

        if (bestAction != null)
        {
            Debug.Log($"Forced decision: {bestAction.actionName} (Utility: {bestUtility:F2})");
            bestAction.Execute(context);
        }
    }
    
    /// <summary>
    /// Debug: Print current context state
    /// </summary>
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
        Debug.Log($"Time Elapsed: {context.GetTimeElapsed():F1}s (Normalized: {context.GetNormalizedTimeElapsed():F2})");
        Debug.Log($"Time Remaining: {context.GetTimeRemaining():F1}s (Normalized: {context.GetNormalizedTimeRemaining():F2})");
        Debug.Log($"Closest Enemy: {context.GetClosestEnemyDistance():F1} (Normalized: {context.GetNormalizedClosestEnemy():F2})");
        Debug.Log($"Player Units: {context.GetPlayerUnitCount()}");
        Debug.Log($"AI Units: {context.GetAIUnitCount()}");
        Debug.Log($"Spawning Enabled: {IsSpawningEnabled()}");
    }
    
    /// <summary>
    /// Debug: Test spawn a specific unit
    /// </summary>
    [ContextMenu("Test Spawn First Available Unit")]
    public void TestSpawn()
    {
        if (spawner == null || aiMoneyManager == null)
        {
            Debug.LogError("Cannot test spawn: Missing spawner or money manager");
            return;
        }

        // Find first spawn action with a unit
        foreach (AIAction action in currentMood.availableActions)
        {
            if (action is SpawnUnitAction spawnAction && spawnAction.unitStats != null)
            {
                Debug.Log($"Test spawning: {spawnAction.unitStats.name}");
                
                if (context == null) InitializeContext();
                context.UpdateContext();
                
                if (spawnAction.CanExecute(context))
                {
                    spawnAction.Execute(context);
                    return;
                }
                else
                {
                    Debug.Log($"Cannot spawn {spawnAction.unitStats.name}: Requirements not met");
                    Debug.Log($"  Cost: {spawnAction.unitStats._spawnCost}, Current Money: {GetCurrentMoney()}");
                }
            }
        }
        
        Debug.Log("No spawn actions available in current mood");
    }
    
    #endregion

    void OnDrawGizmosSelected()
    {
        // Visualize AI range
        if (aiBase != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(aiBase.position, maxDistance * 0.3f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(aiBase.position, maxDistance);
        }

        // Draw lines to zones
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

        // Draw line to spawner
        if (spawner != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, spawner.transform.position);
        }
    }
}