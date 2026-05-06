using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for AI actions
/// </summary>
[System.Serializable]
public abstract class AIAction
{
    public string actionName = "New Action";
    
    [Header("Utility Calculation")]
    [SerializeReference, SubclassSelector]
    public AIConsideration rootConsideration;
    
    /// <summary>
    /// Calculate the utility score for this action
    /// </summary>
    public float CalculateUtility(AIContext context)
    {
        if (rootConsideration == null)
        {
            Debug.LogWarning($"[AI] {actionName} has no root consideration!");
            return 0f;
        }
        
        return rootConsideration.Evaluate(context);
    }
    
    /// <summary>
    /// Execute the action
    /// </summary>
    public abstract IEnumerator Execute(AIContext context, AILoop parentLoop);
    
    /// <summary>
    /// Can this action currently be executed?
    /// </summary>
    public virtual bool CanExecute(AIContext context)
    {
        return true;
    }
}

/// <summary>
/// Spawn a sequence of units with delays between each
/// </summary>
[System.Serializable]
[AddTypeMenu("Spawn Sequence")]
public class SpawnSequenceAction : AIAction
{
    [Header("Spawn Sequence")]
    public List<SpawnStep> spawnSequence = new List<SpawnStep>();
    
    public override bool CanExecute(AIContext context)
    {
        if (spawnSequence.Count == 0)
        {
            Debug.LogWarning($"[AI] {actionName}: No units in spawn sequence!");
            return false;
        }
        
        if (SpawnObjects.EnemyInstance == null)
            return false;
            
        if (!SpawnObjects.EnemyInstance.spawningEnabled)
            return false;
        
        return true;
    }
    
    public override IEnumerator Execute(AIContext context, AILoop parentLoop)
    {
        if (!CanExecute(context))
        {
            if (context.showDebugLogs)
                Debug.Log($"[AI] {actionName}: Cannot execute");
            yield break;
        }
        
        parentLoop.isExecutingSequence = true;
        
        if (context.showDebugLogs)
            Debug.Log($"[AI] ▶ Starting sequence: {actionName} ({spawnSequence.Count} units)");
        
        foreach (SpawnStep step in spawnSequence)
        {
            if (step.unitToSpawn == null)
            {
                Debug.LogWarning($"[AI] {actionName}: Null unit in sequence, skipping");
                continue;
            }
            
            // Spawn the unit
            GameObject spawned = SpawnObjects.EnemyInstance.SpawnFromAISpawner(step.unitToSpawn);
            
            if (spawned != null && context.showDebugLogs)
            {
                Debug.Log($"[AI]   ✓ Spawned {step.unitToSpawn.name}");
            }
            
            // Wait for delay before next spawn
            if (step.delayAfter > 0f)
            {
                if (context.showDebugLogs)
                    Debug.Log($"[AI]   ⏱ Waiting {step.delayAfter}s...");
                    
                yield return new WaitForSeconds(step.delayAfter);
            }
        }
        
        if (context.showDebugLogs)
            Debug.Log($"[AI] ✓ Sequence complete: {actionName}");
        
        parentLoop.isExecutingSequence = false;
    }
}

/// <summary>
/// Do nothing action (useful for having the AI "wait")
/// </summary>
[System.Serializable]
[AddTypeMenu("Do Nothing")]
public class DoNothingAction : AIAction
{
    public override IEnumerator Execute(AIContext context, AILoop parentLoop)
    {
        if (context.showDebugLogs)
            Debug.Log($"[AI] {actionName}: Waiting...");
        
        yield break;
    }
}

/// <summary>
/// Single step in a spawn sequence
/// </summary>
[System.Serializable]
public class SpawnStep
{
    public ScriptableStats unitToSpawn;
    
    [Tooltip("Delay in seconds after spawning this unit")]
    public float delayAfter = 0.5f;
}