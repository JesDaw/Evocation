using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public abstract class AIConsideration
{
    public string considerationName = "New Consideration";
    [Tooltip("Maps normalized input (0-1) to output value")]
    public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public float Evaluate(AIContext context, bool debug, ScriptableStats unitStats)
    {
        float normalizedInput = GetNormalizedValue(context, unitStats);
        float curveOutput = responseCurve.Evaluate(normalizedInput);

        if (debug)
            Debug.Log($"    • {considerationName}: {GetRawValueString(context, unitStats)} → norm={normalizedInput:F2} → curve={curveOutput:F2}");

        return curveOutput;
    }

    protected abstract float GetNormalizedValue(AIContext context, ScriptableStats unitStats);
    protected abstract string GetRawValueString(AIContext context, ScriptableStats unitStats);
}

[System.Serializable]
[AddTypeMenu("Money")]
public class MoneyConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats) => context.GetNormalizedMoney();
    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats) => $"${context.GetCurrentMoney():F1}";
}

[System.Serializable]
[AddTypeMenu("Time Elapsed")]
public class TimeElapsedConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats) => context.GetNormalizedTimeElapsed();
    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats) => $"{context.GetTimeElapsed():F1}s";
}

[System.Serializable]
[AddTypeMenu("Closest Enemy Distance")]
public class ClosestEnemyDistanceConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats) => context.GetNormalizedClosestEnemy();
    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats) => $"{context.GetClosestEnemyDistance():F1}m";
}

[System.Serializable]
[AddTypeMenu("Player Unit Count")]
public class PlayerUnitCountConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats) => context.GetNormalizedPlayerUnits();
    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats) => $"{context.GetPlayerUnitCount()} units";
}

[System.Serializable]
[AddTypeMenu("AI Unit Count")]
public class AIUnitCountConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats) => context.GetNormalizedAIUnits();
    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats) => $"{context.GetAIUnitCount()} units";
}

[System.Serializable]
[AddTypeMenu("Closest Enemy Power Level")]
public class ClosestEnemyPowerLevelConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats) => context.GetNormalizedClosestEnemyPower();
    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats) => $"Power: {context.GetRawClosestEnemyPower():F1}";
}

[System.Serializable]
[AddTypeMenu("Time Since Last Action")]
public class TimeSinceLastActionConsideration : AIConsideration
{
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats) => context.GetNormalizedTimeSinceLastAction();
    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats) => $"{context.GetRawTimeSinceLastAction():F1}s ago";
}

[System.Serializable]
[AddTypeMenu("Zone Pressure")]
public class ZonePressureConsideration : AIConsideration
{
    public ZoneType targetZone = ZoneType.Upper;

    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        float dominance = context.GetNormalizedZoneDominance(targetZone);
        return 1.0f - dominance;
    }

    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        float pressure = 1.0f - context.GetNormalizedZoneDominance(targetZone);
        return $"Pressure in {targetZone}: {pressure:P0}";
    }
}

public enum ZoneType
{
    Upper,
    Middle,
    Lower,
    All
}

[System.Serializable]
[AddTypeMenu("Actions Since Last Picked")]
public class ActionsSinceLastPickedConsideration : AIConsideration
{
    [Tooltip("The name of the action to track (must match the action's actionName in the inspector)")]
    public string actionNameToTrack = "";
    [Tooltip("Max loops to normalize against (e.g., 10 means after 10 loops, utility is 1.0)")]
    public int maxLoopsForNormalization = 10;
    
    protected override float GetNormalizedValue(AIContext context, ScriptableStats unitStats)
    {
        string trackName = string.IsNullOrEmpty(actionNameToTrack) ? considerationName : actionNameToTrack;
        return context.GetLoopsSinceLastPickedNormalized(trackName, maxLoopsForNormalization);
    }
    
    protected override string GetRawValueString(AIContext context, ScriptableStats unitStats)
    {
        string trackName = string.IsNullOrEmpty(actionNameToTrack) ? considerationName : actionNameToTrack;
        int loops = context.GetLoopsSinceLastPicked(trackName);
        return $"{loops} loops ago";
    }
}
