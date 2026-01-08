using UnityEngine;

/// <summary>
/// Individual consideration that actions can evaluate
/// Completely flexible - add any type of consideration you want!
/// </summary>
[CreateAssetMenu(fileName = "NewConsideration", menuName = "AI/Consideration")]
public class AIConsideration : ScriptableObject
{
    [Header("Consideration Info")]
    public string considerationName = "New Consideration";
    
    [Header("Evaluation")]
    [Tooltip("Maps normalized input (0-1) to output value")]
    public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Tooltip("How important is this consideration?")]
    public float weight = 1f;
    
    [Header("Type")]
    public ConsiderationType type;
    
    [Header("Zone-Specific Settings (if applicable)")]
    [Tooltip("Which zone to check (Upper/Middle/Lower)")]
    public ZoneType targetZone;
    
    [Tooltip("Which tag to count in the zone")]
    public string unitTagToCheck = "Player";
    
    [Header("Custom Settings")]
    [Tooltip("For custom considerations - reference to a specific object")]
    public GameObject customTarget;
    
    [Tooltip("Custom threshold value")]
    public float customThreshold = 0f;

    /// <summary>
    /// Evaluate this consideration and return weighted utility
    /// </summary>
    public float Evaluate(AIContext context)
    {
        float normalizedValue = GetNormalizedValue(context);
        float curveValue = responseCurve.Evaluate(normalizedValue);
        return curveValue * weight;
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
                // Implement your own custom logic here
                return EvaluateCustom(context);
                
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Override this or modify for custom considerations
    /// </summary>
    protected virtual float EvaluateCustom(AIContext context)
    {
        // Example: Check if custom target exists and is close
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
/// Add more as needed!
/// </summary>
public enum ConsiderationType
{
    Money,              // How much money AI has
    TimeElapsed,        // How much time has passed
    TimeRemaining,      // How much time is left
    ClosestEnemyDistance, // How close enemies are to base
    PlayerUnitCount,    // Total player units on map
    AIUnitCount,        // Total AI units on map
    UnitsInZone,        // Units in a specific zone
    ZoneDominance,      // Who's winning in a zone (1 = AI, 0 = player)
    Custom              // Custom consideration - extend as needed
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