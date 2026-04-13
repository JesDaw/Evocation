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
    /// Calculate utility by multiplying all consideration outputs
    /// Multiplication acts as a veto - if any consideration is 0, utility becomes 0
    /// </summary>
    public virtual float CalculateUtility(AIContext context, bool debug)
    {
        if (!CanExecute(context))
        {
            if (debug)
                Debug.Log($"✗ {actionName}: CANNOT EXECUTE");
            return 0f;
        }
        
        if (debug)
            Debug.Log($"\n▸ {actionName}:");
        
        if (considerations.Count == 0)
        {
            Debug.LogWarning($"[AI] {actionName} has NO considerations!");
            return 0f;
        }
        
        ScriptableStats unitStats = GetUnitStats();
        
        float totalUtility = 1f;
        
        foreach (AIConsideration consideration in considerations)
        {
            if (consideration == null)
            {
                Debug.LogWarning($"[AI] Null consideration in {actionName}");
                continue;
            }
            
            float value = consideration.Evaluate(context, debug, unitStats);
            totalUtility *= value;
            
            if (totalUtility <= 0f)
            {
                if (debug)
                    Debug.Log($"    ✗ VETOED by {consideration.considerationName}");
                break;
            }
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
    
    public virtual void RecordExecution(AIContext context)
    {
        context.RecordActionExecuted(actionName);
    }
}

/// <summary>
/// Action for spawning a specific unit
/// </summary>
[System.Serializable]
[AddTypeMenu("Spawn Unit")]
public class SpawnUnitAction : AIAction
{
    public ScriptableStats unitToSpawn;

    private float lastSpawnTime = -999f;

    protected override ScriptableStats GetUnitStats()
    {
        return unitToSpawn;
    }

    public override bool CanExecute(AIContext context)
    {
        if (unitToSpawn == null)
        {
            Debug.LogWarning($"[AI] {actionName}: No unitToSpawn assigned!");
            return false;
        }
        
        if (context.spawner == null)
        {
            Debug.LogWarning($"[AI] {actionName}: No spawner!");
            return false;
        }
        
        if (!context.spawner.spawningEnabled)
            return false;
        
        // Binary check: can we afford it?
        if (context.GetCurrentMoney() < unitToSpawn._spawnCost)
            return false;
        
        return true;
    }

    public override void Execute(AIContext context)
    {
        if (!CanExecute(context)) return;

        GameObject spawnedUnit = context.spawner.SpawnFromSpawner(unitToSpawn);
        
        if (spawnedUnit != null && context.aiMoneyManager != null)
        {
            context.aiMoneyManager.SpendMoney(unitToSpawn._spawnCost);
            lastSpawnTime = Time.time;
            
            if (context.showDebugLogs)
                Debug.Log($"[AI] ✓ Spawned {unitToSpawn.name} (Cost: {unitToSpawn._spawnCost})");
            
            RecordExecution(context);
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