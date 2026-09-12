using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public abstract class AIAction
{
    public string actionName = "New Action";

    [Header("Utility Calculation")]
    [SerializeReference, SubclassSelector]
    public AIConsideration rootConsideration;

    public float CalculateUtility(AIClanSO clanConfig)
    {
        if (rootConsideration == null)
        {
            Debug.LogWarning($"[AI] {actionName} has no root consideration!");
            return 0f;
        }

        return rootConsideration.Evaluate(clanConfig);
    }

    public abstract IEnumerator Execute(AIClanSO clanConfig, AILoop parentLoop);

    public virtual bool CanExecute(AIClanSO clanConfig)
    {
        return true;
    }
}

[System.Serializable]
[AddTypeMenu("Spawn Sequence")]
public class SpawnSequenceAction : AIAction
{
    [Header("Spawn Sequence")]
    public List<SpawnStep> spawnSequence = new List<SpawnStep>();
    public override bool CanExecute(AIClanSO clanConfig)
    {
        if (spawnSequence.Count == 0)
        {
            Debug.LogWarning($"[AI] {actionName}: No units in spawn sequence!");
            return false;
        }
        if (SpawnObjects.EnemyInstance == null) return false;
        if (!SpawnObjects.EnemyInstance.spawningEnabled) return false;

        return true;
    }

    public override IEnumerator Execute(AIClanSO clanConfig, AILoop parentLoop) //neather of these inputs are needed
    {
        if (!CanExecute(clanConfig))
        {
            if (parentLoop.showDebugLogs) Debug.Log($"[AI] {actionName}: Cannot execute");
            yield break;
        }
        parentLoop.isExecutingSequence = true;
        if (parentLoop.showDebugLogs) Debug.Log($"[AI] ▶ Starting sequence: {actionName} ({spawnSequence.Count} units)");

        foreach (SpawnStep step in spawnSequence)
        {
            if (step.unitToSpawn == null)
            {
                Debug.LogWarning($"[AI] {actionName}: Null unit in sequence, skipping");
                continue;
            }

            GameObject spawned = SpawnObjects.EnemyInstance.Spawn(step.unitToSpawn, true);

            if (spawned != null && parentLoop.showDebugLogs)
                Debug.Log($"[AI]   ✓ Spawned {step.unitToSpawn.name}");

            if (step.delayAfter > 0f)
            {
                if (parentLoop.showDebugLogs) Debug.Log($"[AI]   ⏱ Waiting {step.delayAfter}s...");
                yield return new WaitForSeconds(step.delayAfter);
            }
        }

        if (parentLoop.showDebugLogs) Debug.Log($"[AI] ✓ Sequence complete: {actionName}");
        parentLoop.isExecutingSequence = false;
    }
}

[System.Serializable]
[AddTypeMenu("Do Nothing")]
public class DoNothingAction : AIAction
{
    public override IEnumerator Execute(AIClanSO clanConfig, AILoop parentLoop)
    {
        if (parentLoop.showDebugLogs) Debug.Log($"[AI] {actionName}: Waiting...");
        yield break;
    }
}

[System.Serializable]
public class SpawnStep
{
    public ScriptableStats unitToSpawn;

    [Tooltip("Delay in seconds after spawning this unit")]
    public float delayAfter = 0.5f;
}