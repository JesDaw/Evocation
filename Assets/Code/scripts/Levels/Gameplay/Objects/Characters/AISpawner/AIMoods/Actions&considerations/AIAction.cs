using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for AI actions with clean debug logging
/// </summary>
[System.Serializable]
public abstract class AIAction
{
    public string actionName = "AI Action";
    [SerializeReference, SubclassSelector]
    public List<AIConsideration> considerations = new List<AIConsideration>();

    /// <summary>
    /// Calculate utility by summing all consideration outputs
    /// </summary>
    public virtual float CalculateUtility(AIContext context, bool debug)
    {
        if (!CanExecute(context))
        {
            if (debug)
                Debug.Log($"✗ {actionName}: CANNOT EXECUTE");
            return float.MinValue;
        }
        
        if (debug)
            Debug.Log($"\n▸ {actionName}:");
        
        if (considerations.Count == 0)
        {
            Debug.LogWarning($"[AI] {actionName} has NO considerations!");
            return 0f;
        }
        
        // Get unit stats if this is a spawn action
        ScriptableStats unitStats = GetUnitStats();
        
        float totalUtility = 0f;
        
        foreach (AIConsideration consideration in considerations)
        {
            if (consideration == null)
            {
                Debug.LogWarning($"[AI] Null consideration in {actionName}");
                continue;
            }
            
            float value = consideration.Evaluate(context, debug, unitStats);
            totalUtility += value;
        }
        
        if (debug)
            Debug.Log($"  TOTAL UTILITY: {totalUtility:F2}");
        
        return totalUtility;
    }

    /// <summary>
    /// Override in derived classes to provide unit stats for CanAffordUnit considerations
    /// </summary>
    protected virtual ScriptableStats GetUnitStats()
    {
        return null;
    }

    public abstract void Execute(AIContext context);
    public virtual bool CanExecute(AIContext context) => true;
}

/// <summary>
/// Action for spawning a specific unit
/// </summary>
[System.Serializable]
[AddTypeMenu("Spawn Unit")]
public class SpawnUnitAction : AIAction
{
    public ScriptableStats unitStats;

    [Header("Spawn Requirements")]
    private float lastSpawnTime = -999f;

    protected override ScriptableStats GetUnitStats()
    {
        return unitStats;
    }

    public override bool CanExecute(AIContext context)
    {
        if (unitStats == null)
        {
            Debug.LogWarning($"[AI] {actionName}: No unitStats assigned!");
            return false;
        }
        
        if (context.spawner == null)
        {
            Debug.LogWarning($"[AI] {actionName}: No spawner!");
            return false;
        }
        
        if (!context.spawner.spawningEnabled)
            return false;
        
        if (context.GetCurrentMoney() < unitStats._spawnCost)
            return false;
        
        return true;
    }

    public override void Execute(AIContext context)
    {
        if (!CanExecute(context)) return;

        GameObject spawnedUnit = context.spawner.SpawnFromSpawner(unitStats);
        
        if (spawnedUnit != null && context.aiMoneyManager != null)
        {
            context.aiMoneyManager.SpendMoney(unitStats._spawnCost);
            lastSpawnTime = Time.time;
            
            if (context.showDebugLogs)
                Debug.Log($"[AI] ✓ Spawned {unitStats.name} (Cost: {unitStats._spawnCost})");
        }
    }
}

/// <summary>
/// Action for waiting/saving money
/// </summary>
[System.Serializable]
[AddTypeMenu("Do Nothing")]
public class DoNothingAction : AIAction
{
    public override void Execute(AIContext context)
    {
        if (context.showDebugLogs)
            Debug.Log($"[AI] Waiting... (Money: {context.GetCurrentMoney():F1})");
    }

    public override bool CanExecute(AIContext context) => true;
}