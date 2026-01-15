using UnityEngine;

/// <summary>
/// Base class for AI considerations - now supports subclassing for clean inspector
/// </summary>
[System.Serializable]
public abstract class AIConsideration
{
    [Header("Consideration Info")]
    public string considerationName = "New Consideration";

    [Header("Evaluation")]
    [Tooltip("Maps normalized input (0-1) to output value")]
    public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);

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
    /// Get normalized (0-1) value - implemented by subclasses
    /// </summary>
    protected abstract float GetNormalizedValue(AIContext context, ScriptableStats unitStats);

    /// <summary>
    /// Get the raw value as a readable string for debugging - implemented by subclasses
    /// </summary>
    protected abstract string GetRawValueString(AIContext context, ScriptableStats unitStats);
}

/// <summary>
/// Consideration for current AI money amount
/// </summary>
[System.Serializable]
[AddTypeMenu("Money")]
public class MoneyConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        return context.GetNormalizedMoney();
    }

    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        return $"${context.GetCurrentMoney():F1}";
    }
}

/// <summary>
/// Consideration for whether AI can afford a specific unit
/// </summary>
[System.Serializable]
[AddTypeMenu("Can Afford Unit")]
public class CanAffordUnitConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        if (unitStats == null)
        {
            Debug.LogWarning(
                $"[AI] {considerationName}: No unitStats provided for CanAffordUnit!"
            );
            return 0f;
        }
        return context.GetCurrentMoney() >= unitStats._spawnCost ? 1f : 0f;
    }

    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        if (unitStats != null)
        {
            return $"${context.GetCurrentMoney():F1} vs ${unitStats._spawnCost}";
        }
        return "NO UNIT STATS";
    }
}

/// <summary>
/// Consideration for time elapsed since level start
/// </summary>
[System.Serializable]
[AddTypeMenu("Time Elapsed")]
public class TimeElapsedConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        return context.GetNormalizedTimeElapsed();
    }

    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        return $"{context.GetTimeElapsed():F1}s";
    }
}

/// <summary>
/// Consideration for time remaining in level
/// </summary>
[System.Serializable]
[AddTypeMenu("Time Remaining")]
public class TimeRemainingConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        return context.GetNormalizedTimeRemaining();
    }

    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        return $"{context.GetTimeRemaining():F1}s";
    }
}

/// <summary>
/// Consideration for distance to closest enemy
/// </summary>
[System.Serializable]
[AddTypeMenu("Closest Enemy Distance")]
public class ClosestEnemyDistanceConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        return context.GetNormalizedClosestEnemy();
    }

    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        return $"{context.GetClosestEnemyDistance():F1}";
    }
}

/// <summary>
/// Consideration for number of player units
/// </summary>
[System.Serializable]
[AddTypeMenu("Player Unit Count")]
public class PlayerUnitCountConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        return context.GetNormalizedPlayerUnits();
    }

    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        return $"{context.GetPlayerUnitCount()} units";
    }
}

/// <summary>
/// Consideration for number of AI units
/// </summary>
[System.Serializable]
[AddTypeMenu("AI Unit Count")]
public class AIUnitCountConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        return context.GetNormalizedAIUnits();
    }

    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        return $"{context.GetAIUnitCount()} units";
    }
}

/// <summary>
/// Consideration for number of units in a specific zone
/// </summary>
[System.Serializable]
[AddTypeMenu("Units In Zone")]
public class UnitsInZoneConsideration : AIConsideration
{
    [Header("Zone Settings")]
    public ZoneType targetZone = ZoneType.Upper;
    public string unitTagToCheck = "Player";

    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        return context.GetNormalizedUnitsInZone(targetZone, unitTagToCheck);
    }

    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        return $"{context.GetUnitsInZone(targetZone, unitTagToCheck)} units in {targetZone}";
    }
}

/// <summary>
/// Consideration for zone dominance (AI units vs player units in zone)
/// </summary>
[System.Serializable]
[AddTypeMenu("Zone Dominance")]
public class ZoneDominanceConsideration : AIConsideration
{
    [Header("Zone Settings")]
    public ZoneType targetZone = ZoneType.Upper;

    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        return context.GetZoneDominance(targetZone);
    }

    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        return $"Dominance: {context.GetZoneDominance(targetZone):F2} in {targetZone}";
    }
}

public enum ZoneType
{
    Upper,
    Middle,
    Lower,
    All
}