using UnityEngine;

/// <summary>
/// Individual consideration with clean debug output
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
    
    [Header("Zone-Specific Settings")]
    public ZoneType targetZone = ZoneType.Upper;
    public string unitTagToCheck = "Player";

    /// <summary>
    /// Evaluate and return curve output
    /// </summary>
    public float Evaluate(AIContext context, bool debug, ScriptableStats unitStats)
    {
        float normalizedInput = GetNormalizedValue(context, unitStats);
        float curveOutput = responseCurve.Evaluate(normalizedInput);

        if (debug)
        {
            string rawValue = GetRawValueString(context, unitStats);
            Debug.Log(
                $"    • {considerationName}: {rawValue} → norm={normalizedInput:F2} → curve={curveOutput:F2}"
            );
        }

        return curveOutput;
    }


    /// <summary>
    /// Get the raw value as a readable string for debugging
    /// </summary>
    private string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        switch (type)
        {
            case ConsiderationType.Money:
                return $"${context.GetCurrentMoney():F1}";

            case ConsiderationType.CanAffordUnit:
                if (unitStats != null)
                {
                    return $"${context.GetCurrentMoney():F1} vs ${unitStats._spawnCost}";
                }
                return "NO UNIT STATS";

            case ConsiderationType.TimeElapsed:
                return $"{context.GetTimeElapsed():F1}s";

            case ConsiderationType.TimeRemaining:
                return $"{context.GetTimeRemaining():F1}s";

            case ConsiderationType.ClosestEnemyDistance:
                return $"{context.GetClosestEnemyDistance():F1}";

            case ConsiderationType.PlayerUnitCount:
                return $"{context.GetPlayerUnitCount()} units";

            case ConsiderationType.AIUnitCount:
                return $"{context.GetAIUnitCount()} units";

            default:
                return "N/A";
        }
    }


    /// <summary>
    /// Get normalized (0-1) value based on consideration type
    /// </summary>
    private float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        switch (type)
        {
            case ConsiderationType.Money:
                return context.GetNormalizedMoney();

            case ConsiderationType.CanAffordUnit:
                if (unitStats == null)
                {
                    Debug.LogWarning(
                        $"[AI] {considerationName}: No unitStats provided for CanAffordUnit!"
                    );
                    return 0f;
                }
                return context.GetCurrentMoney() >= unitStats._spawnCost ? 1f : 0f;

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

            default:
                return 0f;
        }
    }
}

public enum ConsiderationType
{
    Money,
    CanAffordUnit,  // NEW: Checks if money >= unit spawn cost
    TimeElapsed,
    TimeRemaining,
    ClosestEnemyDistance,
    PlayerUnitCount,
    AIUnitCount,
    UnitsInZone,
    ZoneDominance
}

public enum ZoneType
{
    Upper,
    Middle,
    Lower,
    All
}