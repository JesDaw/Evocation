using UnityEngine;

/// <summary>
/// Individual consideration for AI actions
/// Now a serializable class - edit directly in inspector!
/// No ScriptableObject needed
/// </summary>
[System.Serializable]
public class AIConsideration
{
    [Header("Consideration Info")]
    public string considerationName = "New Consideration";
    
    [Header("Type")]
    public ConsiderationType type;
    
    [Header("Evaluation")]
    [Tooltip("Maps normalized input (0-1) to output value")]
    public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Tooltip("How important is this consideration?")]
    public float weight = 1f;
    
    [Header("Zone-Specific Settings (if applicable)")]
    [Tooltip("Which zone to check")]
    public ZoneType targetZone = ZoneType.Upper;
    
    [Tooltip("Which tag to count in the zone")]
    public string unitTagToCheck = "Player";
    
    [Header("Custom Settings")]
    [Tooltip("For custom considerations")]
    public GameObject customTarget;
    public float customThreshold = 0f;

    /// <summary>
    /// Evaluate this consideration and return weighted utility
    /// </summary>
    public float Evaluate(AIContext context)
    {
        float normalizedValue = GetNormalizedValue(context);
        float curveValue = responseCurve.Evaluate(normalizedValue);
        float result = curveValue * weight;
        
        if (Application.isEditor)
            Debug.Log($"      {considerationName}: normalized={normalizedValue:F2}, curve={curveValue:F2}, weight={weight:F2}, result={result:F2}");
        
        return result;
    }

    /// <summary>
    /// Get normalized (0-1) value based on consideration type
    /// </summary>
    private float GetNormalizedValue(AIContext context)
    {
        switch (type)
        {
            case ConsiderationType.Money:
                return context.GetNormalizedMoney();
                
            case ConsiderationType.TimeElapsed:
                return context.GetNormalizedTimeElapsed();
                
            case ConsiderationType.TimeRemaining:
                return context.GetNormalizedTimeRemaining();
                
            case ConsiderationType.ClosestEnemyDistance:
                return context.GetNormalizedClosestEnemy();
                
            case ConsiderationType.PlayerUnitCount:
                return context.GetNormalizedPlayerUnits();
                
            case ConsiderationType.AIUnitCount:
                return context.GetNormalizedAIUnits();
                
            case ConsiderationType.UnitsInZone:
                return context.GetNormalizedUnitsInZone(targetZone, unitTagToCheck);
                
            case ConsiderationType.ZoneDominance:
                return context.GetZoneDominance(targetZone);
                
            case ConsiderationType.Custom:
                return EvaluateCustom(context);
                
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Override this for custom considerations
    /// </summary>
    protected virtual float EvaluateCustom(AIContext context)
    {
        if (customTarget != null && context.aiBase != null)
        {
            float distance = Vector3.Distance(customTarget.transform.position, context.aiBase.position);
            return Mathf.Clamp01(1f - (distance / context.maxDistance));
        }
        return 0f;
    }
}

/// <summary>
/// Types of considerations the AI can evaluate
/// </summary>
public enum ConsiderationType
{
    Money,
    TimeElapsed,
    TimeRemaining,
    ClosestEnemyDistance,
    PlayerUnitCount,
    AIUnitCount,
    UnitsInZone,
    ZoneDominance,
    Custom
}

/// <summary>
/// Which map zone to check
/// </summary>
public enum ZoneType
{
    Upper,
    Middle,
    Lower,
    All
}