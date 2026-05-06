using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Base consideration class
/// </summary>
[System.Serializable]
public abstract class AIConsideration
{
    public string considerationName = "New Consideration";
    
    [Tooltip("Maps normalized input (0-1) to output value (0-1)")]
    public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    /// <summary>
    /// Evaluate this consideration and return a 0-1 utility score
    /// </summary>
    public abstract float Evaluate(AIContext context);
    
    /// <summary>
    /// Get a human-readable debug string
    /// </summary>
    public virtual string GetDebugString(AIContext context)
    {
        return $"{considerationName}: {Evaluate(context):F2}";
    }
}

/// <summary>
/// Combines multiple considerations using different mathematical operations
/// </summary>
[System.Serializable]
[AddTypeMenu("Composite")]
public class CompositeConsideration : AIConsideration
{
    [Header("Child Considerations")]
    [SerializeReference, SubclassSelector]
    public List<AIConsideration> children = new List<AIConsideration>();
    
    [Header("Combination Method")]
    public CombineMode combineMode = CombineMode.Multiply;
    
    public override float Evaluate(AIContext context)
    {
        if (children == null || children.Count == 0)
        {
            Debug.LogWarning($"[AI] {considerationName}: No child considerations!");
            return 0f;
        }
        
        List<float> values = new List<float>();
        
        foreach (var child in children)
        {
            if (child == null)
            {
                Debug.LogWarning($"[AI] {considerationName}: Null child consideration!");
                continue;
            }
            
            values.Add(child.Evaluate(context));
        }
        
        if (values.Count == 0)
            return 0f;
        
        float result = 0f;
        
        switch (combineMode)
        {
            case CombineMode.Average:
                result = values.Average();
                break;
                
            case CombineMode.Multiply:
                result = 1f;
                foreach (float v in values)
                    result *= v;
                break;
                
            case CombineMode.Add:
                result = values.Sum();
                result = Mathf.Clamp01(result); // Keep in 0-1 range
                break;
                
            case CombineMode.Max:
                result = values.Max();
                break;
                
            case CombineMode.Min:
                result = values.Min();
                break;
        }
        
        // Apply response curve to final result
        return responseCurve.Evaluate(result);
    }
    
    public override string GetDebugString(AIContext context)
    {
        string childrenStr = string.Join(", ", children.Select(c => c?.GetDebugString(context) ?? "null"));
        return $"{considerationName} ({combineMode}): [{childrenStr}] = {Evaluate(context):F2}";
    }
}

public enum CombineMode
{
    Average,    // Average of all values
    Multiply,   // Multiply all values (acts as AND - any 0 makes result 0)
    Add,        // Sum all values (clamped to 1.0)
    Max,        // Take highest value
    Min         // Take lowest value
}

// ========== CONCRETE CONSIDERATIONS ==========

[System.Serializable]
[AddTypeMenu("Time Elapsed")]
public class TimeElapsedConsideration : AIConsideration
{
    public override float Evaluate(AIContext context)
    {
        float normalized = context.GetNormalizedTimeElapsed();
        return responseCurve.Evaluate(normalized);
    }
    
    public override string GetDebugString(AIContext context)
    {
        return $"{considerationName}: {context.GetTimeElapsed():F1}s → {Evaluate(context):F2}";
    }
}

[System.Serializable]
[AddTypeMenu("Player Unit Count")]
public class PlayerUnitCountConsideration : AIConsideration
{
    public override float Evaluate(AIContext context)
    {
        float normalized = context.GetNormalizedPlayerUnits();
        return responseCurve.Evaluate(normalized);
    }
    
    public override string GetDebugString(AIContext context)
    {
        return $"{considerationName}: {context.GetPlayerUnitCount()} units → {Evaluate(context):F2}";
    }
}

[System.Serializable]
[AddTypeMenu("Zone Pressure")]
public class ZonePressureConsideration : AIConsideration
{
    public ZoneType targetZone = ZoneType.Upper;
    
    public override float Evaluate(AIContext context)
    {
        // Zone pressure = 1 - our dominance (high when enemy is winning)
        float dominance = context.GetNormalizedZoneDominance(targetZone);
        float pressure = 1f - dominance;
        return responseCurve.Evaluate(pressure);
    }
    
    public override string GetDebugString(AIContext context)
    {
        float pressure = 1f - context.GetNormalizedZoneDominance(targetZone);
        return $"{considerationName} ({targetZone}): {pressure:P0} → {Evaluate(context):F2}";
    }
}

[System.Serializable]
[AddTypeMenu("Closest Enemy Distance")]
public class ClosestEnemyDistanceConsideration : AIConsideration
{
    public override float Evaluate(AIContext context)
    {
        float normalized = context.GetNormalizedClosestEnemy();
        return responseCurve.Evaluate(normalized);
    }
    
    public override string GetDebugString(AIContext context)
    {
        return $"{considerationName}: {context.GetClosestEnemyDistance():F1}m → {Evaluate(context):F2}";
    }
}

[System.Serializable]
[AddTypeMenu("Closest Enemy Power")]
public class ClosestEnemyPowerConsideration : AIConsideration
{
    public override float Evaluate(AIContext context)
    {
        float normalized = context.GetNormalizedClosestEnemyPower();
        return responseCurve.Evaluate(normalized);
    }
    
    public override string GetDebugString(AIContext context)
    {
        return $"{considerationName}: {context.GetRawClosestEnemyPower():F1} → {Evaluate(context):F2}";
    }
}

public enum ZoneType
{
    Upper,
    Middle,
    Lower
}