using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for AI actions
/// Now serializable - define inline in inspector!
/// </summary>
[System.Serializable]
public abstract class AIAction
{
    [Header("Action Info")]
    public string actionName = "AI Action";
    
    [TextArea(2, 4)]
    public string description = "What does this action do?";
    
    [Header("Considerations")]
    [Tooltip("All factors this action considers")]
    public List<AIConsideration> considerations = new List<AIConsideration>();
    
    [Header("Bonus Utility")]
    [Tooltip("Flat bonus added to utility (use sparingly)")]
    public float flatBonus = 0f;

    /// <summary>
    /// Calculate utility by evaluating all considerations
    /// </summary>
    public virtual float CalculateUtility(AIContext context)
    {
        if (!CanExecute(context))
            return float.MinValue;
        
        float totalUtility = flatBonus;
        
        foreach (AIConsideration consideration in considerations)
        {
            if (consideration != null)
            {
                float considerationValue = consideration.Evaluate(context);
                totalUtility += considerationValue;
                
                if (Application.isEditor)
                    Debug.Log($"    {consideration.considerationName}: {considerationValue:F2} (weight: {consideration.weight})");
            }
        }
        
        if (Application.isEditor && considerations.Count == 0)
            Debug.LogWarning($"  {actionName} has NO considerations! Add at least one.");
        
        return totalUtility;
    }

    /// <summary>
    /// Execute this action - implement in derived classes
    /// </summary>
    public abstract void Execute(AIContext context);

    /// <summary>
    /// Can this action be executed right now?
    /// </summary>
    public virtual bool CanExecute(AIContext context)
    {
        return true;
    }
}

/// <summary>
/// Action for spawning a specific unit
/// </summary>
[System.Serializable]
public class SpawnUnitAction : AIAction
{
    [Header("Unit to Spawn")]
    public ScriptableStats unitStats;

    [Header("Spawn Requirements")]
    [Tooltip("Minimum money required")]
    public float minMoneyThreshold = 0f;
    
    [Tooltip("Can't spawn if we have this many or more units")]
    public int maxOwnUnits = 999;
    
    [Header("Spawn Cooldown")]
    [Tooltip("Minimum seconds between spawns (0 = no cooldown)")]
    public float spawnCooldown = 0f;
    
    private float lastSpawnTime = -999f;

    public override bool CanExecute(AIContext context)
    {
        if (unitStats == null)
        {
            if (Application.isEditor)
                Debug.LogWarning($"{actionName}: No unitStats assigned!");
            return false;
        }
        
        if (context.spawner == null)
        {
            if (Application.isEditor)
                Debug.LogWarning($"{actionName}: No spawner in context!");
            return false;
        }
        
        if (!context.spawner.spawningEnabled)
        {
            if (Application.isEditor)
                Debug.Log($"{actionName}: Spawning is disabled");
            return false;
        }
        
        float currentMoney = context.GetCurrentMoney();
        
        if (currentMoney < unitStats._spawnCost)
        {
            if (Application.isEditor)
                Debug.Log($"{actionName}: Not enough money! Need {unitStats._spawnCost}, have {currentMoney:F1}");
            return false;
        }
        
        if (currentMoney < minMoneyThreshold)
        {
            if (Application.isEditor)
                Debug.Log($"{actionName}: Below minimum threshold! Need {minMoneyThreshold}, have {currentMoney:F1}");
            return false;
        }
        
        if (context.GetAIUnitCount() >= maxOwnUnits)
        {
            if (Application.isEditor)
                Debug.Log($"{actionName}: Too many units! {context.GetAIUnitCount()}/{maxOwnUnits}");
            return false;
        }
        
        if (spawnCooldown > 0f && Time.time - lastSpawnTime < spawnCooldown)
        {
            if (Application.isEditor)
                Debug.Log($"{actionName}: On cooldown! {(spawnCooldown - (Time.time - lastSpawnTime)):F1}s remaining");
            return false;
        }
        
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
            
            if (Application.isEditor)
                Debug.Log($"AI spawned {unitStats.name} (Cost: {unitStats._spawnCost})");
        }
    }
}

/// <summary>
/// Action for waiting/saving money
/// </summary>
[System.Serializable]
public class DoNothingAction : AIAction
{
    [Header("Wait Message")]
    public string waitMessage = "AI is saving money...";

    public override void Execute(AIContext context)
    {
        if (!string.IsNullOrEmpty(waitMessage) && Application.isEditor)
        {
            Debug.Log($"{waitMessage} (Money: {context.GetCurrentMoney():F1})");
        }
    }

    public override bool CanExecute(AIContext context)
    {
        return true;
    }
}