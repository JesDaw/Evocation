using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Main AI controller that manages multiple decision loops
/// Each loop operates independently with its own timing
/// </summary>
public class AISpawnerController : MonoBehaviour
{
    [Header("AI Configuration")]
    [Tooltip("The AI clan scriptable object containing all behavior")]
    public AIClanSO aiClan;
    
    [Header("Game Systems")]
    [SerializeField] private Timer gameTimer;
    
    [Header("Map Zones")]
    [SerializeField] private MapZonesManager upperZone;
    [SerializeField] private MapZonesManager middleZone;
    [SerializeField] private MapZonesManager lowerZone;
    
    [Header("Spatial")]
    [SerializeField] private Transform aiBase;
    [SerializeField] private Transform playerBase;
    
    [Header("Normalization Settings")]
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private float maxUnits = 20f;
    [SerializeField] private float maxEnemyPower = 50f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    public static AISpawnerController Instance { get; private set; }
    
    private AIContext context;
    private bool isRunning = false;
    private int currentMoodIndex = 0;
    private AILoop[] currentLoops;
    
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
        ValidateSetup();
        InitializeContext();
    }
    
    void ValidateSetup()
    {
        if (aiClan == null)
        {
            Debug.LogError("[AI] No AIClanScriptable assigned!");
            return;
        }
        
        if (currentLoops == null || currentLoops.Length == 0)
        {
            Debug.LogError($"[AI] {aiClan.clanName} has no decision loops!");
            return;
        }
        
        if (SpawnObjects.EnemyInstance == null)
            Debug.LogError("[AI] No enemy spawner found!");
    }
    
    void InitializeContext()
    {
        float calculatedMaxDistance = (aiBase != null && playerBase != null) 
            ? Vector3.Distance(aiBase.position, playerBase.position) 
            : maxDistance;
        
        context = new AIContext
        {
            timer = gameTimer,
            upperZone = upperZone,
            middleZone = middleZone,
            lowerZone = lowerZone,
            aiBase = aiBase,
            playerBase = playerBase,
            maxDistance = calculatedMaxDistance,
            maxUnits = maxUnits,
            maxEnemyPower = maxEnemyPower,
            showDebugLogs = showDebugLogs
        };
        
        if (aiClan.moods != null && aiClan.moods.Count > 0)
        {
            currentMoodIndex = aiClan.startingMoodIndex;
            SetCurrentLoops();
        }
    }
    
    private void SetCurrentLoops()
    {
        if (aiClan == null || aiClan.moods == null || aiClan.moods.Count == 0)
        {
            currentLoops = null;
            return;
        }
        
        currentMoodIndex = Mathf.Clamp(currentMoodIndex, 0, aiClan.moods.Count - 1);
        var mood = aiClan.moods[currentMoodIndex];
        currentLoops = mood.decisionLoops.ToArray();
    }
    
    public AIPersonality GetCurrentMood()
    {
        if (aiClan == null || aiClan.moods == null || aiClan.moods.Count == 0)
            return null;
        return aiClan.moods[currentMoodIndex];
    }
    
    public void SetMoodByName(string moodName)
    {
        if (aiClan == null || aiClan.moods == null)
            return;
        
        for (int i = 0; i < aiClan.moods.Count; i++)
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
                
                if (showDebugLogs)
                    Debug.Log($"[AI] Switched to mood '{moodName}'");
                return;
            }
        }
        Debug.LogWarning($"[AI] Mood '{moodName}' not found!");
    }
    
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
        
        // Initialize all loops
        foreach (var loop in currentLoops)
        {
            loop.Initialize();
        }
        
        // Start update coroutine
        StartCoroutine(UpdateAllLoops());
        
        if (showDebugLogs)
            Debug.Log($"[AI] Started {aiClan.clanName} with {currentLoops.Length} loops");
    }
    
    public void StopAI()
    {
        isRunning = false;
        StopAllCoroutines();
        
        if (showDebugLogs)
            Debug.Log("[AI] Stopped");
    }
    
    /// <summary>
    /// Main update loop that manages all decision loops
    /// </summary>
    private IEnumerator UpdateAllLoops()
    {
        while (isRunning)
        {
            // Update context once per frame
            context.UpdateContext();
            
            // Check each loop
            foreach (var loop in currentLoops)
            {
                if (loop.UpdateTimer(Time.deltaTime))
                {
                    // Time to make a decision!
                    StartCoroutine(ExecuteLoop(loop));
                    loop.ResetTimer();
                }
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Execute one decision cycle for a loop
    /// </summary>
    private IEnumerator ExecuteLoop(AILoop loop)
    {
        if (showDebugLogs)
            Debug.Log($"\n=== LOOP: {loop.loopName} ===");
        
        // Find best action
        AIAction bestAction = null;
        float bestUtility = -1f;
        
        foreach (var action in loop.possibleActions)
        {
            if (action == null)
            {
                Debug.LogWarning($"[AI] Null action in loop {loop.loopName}");
                continue;
            }
            
            if (!action.CanExecute(context))
            {
                if (showDebugLogs)
                    Debug.Log($"  ✗ {action.actionName}: Cannot execute");
                continue;
            }
            
            float utility = action.CalculateUtility(context);
            
            if (showDebugLogs)
            {
                if (action.rootConsideration != null)
                    Debug.Log($"  • {action.actionName}: {action.rootConsideration.GetDebugString(context)}");
                else
                    Debug.Log($"  • {action.actionName}: {utility:F2}");
            }
            
            if (utility > bestUtility)
            {
                bestUtility = utility;
                bestAction = action;
            }
        }
        
        // Execute best action
        if (bestAction != null && bestUtility > 0f)
        {
            if (showDebugLogs)
                Debug.Log($"→ CHOSEN: {bestAction.actionName} (Utility: {bestUtility:F2})");
            
            yield return StartCoroutine(bestAction.Execute(context, loop));
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"→ NO VALID ACTION (best utility: {bestUtility:F2})");
        }
    }
    
    /// <summary>
    /// Add a delay to a specific loop (useful for boss pauses, etc)
    /// </summary>
    public void AddDelayToLoop(string loopName, float additionalTime)
    {
        var loop = currentLoops.FirstOrDefault(l => l.loopName == loopName);
        if (loop != null)
        {
            loop.AddDelay(additionalTime);
            if (showDebugLogs)
                Debug.Log($"[AI] Added {additionalTime}s delay to loop '{loopName}'");
        }
        else
        {
            Debug.LogWarning($"[AI] Loop '{loopName}' not found!");
        }
    }
    
    /// <summary>
    /// Add delay to all loops
    /// </summary>
    public void AddDelayToAllLoops(float additionalTime)
    {
        foreach (var loop in currentLoops)
        {
            loop.AddDelay(additionalTime);
        }
        
        if (showDebugLogs)
            Debug.Log($"[AI] Added {additionalTime}s delay to all loops");
    }
    
    #region Debug Methods
    
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
        Debug.Log($"Time Elapsed: {context.GetTimeElapsed():F1}s (Norm: {context.GetNormalizedTimeElapsed():F2})");
        Debug.Log($"Time Remaining: {context.GetTimeRemaining():F1}s (Norm: {context.GetNormalizedTimeRemaining():F2})");
        Debug.Log($"Player Units: {context.GetPlayerUnitCount()} (Norm: {context.GetNormalizedPlayerUnits():F2})");
        Debug.Log($"AI Units: {context.GetAIUnitCount()} (Norm: {context.GetNormalizedAIUnits():F2})");
        Debug.Log($"Closest Enemy: {context.GetClosestEnemyDistance():F1}m (Norm: {context.GetNormalizedClosestEnemy():F2})");
        Debug.Log($"Closest Enemy Power: {context.GetRawClosestEnemyPower():F1} (Norm: {context.GetNormalizedClosestEnemyPower():F2})");
        Debug.Log("====================\n");
    }
    
    [ContextMenu("Print Loop States")]
    public void PrintLoopStates()
    {
        if (currentLoops == null || currentLoops.Length == 0)
        {
            Debug.Log("[AI] No loops to display");
            return;
        }
        
        Debug.Log("\n=== LOOP STATES ===");
        foreach (var loop in currentLoops)
        {
            Debug.Log($"{loop.loopName}:");
            Debug.Log($"  Timer: {loop.currentTimer:F2}/{loop.currentInterval:F2}s");
            Debug.Log($"  Executing Sequence: {loop.isExecutingSequence}");
            Debug.Log($"  Available Actions: {loop.possibleActions.Count}");
        }
        Debug.Log("==================\n");
    }
    
    #endregion
}