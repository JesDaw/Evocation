using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public abstract class AIConsideration
{
    public string considerationName = "New Consideration";

    [Tooltip("Maps normalized input (0-1) to output value (0-1)")]
    public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public abstract float Evaluate(AIClanSO clanConfig);

    public virtual string GetDebugString(AIClanSO clanConfig)
    {
        return $"{considerationName}: {Evaluate(clanConfig):F2}";
    }
}

[System.Serializable]
[AddTypeMenu("Composite")]
public class CompositeConsideration : AIConsideration
{
    [Header("Child Considerations")]
    [SerializeReference, SubclassSelector]
    public List<AIConsideration> children = new List<AIConsideration>();

    [Header("Combination Method")]
    public CombineMode combineMode = CombineMode.Multiply;

    public override float Evaluate(AIClanSO clanConfig)
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
            values.Add(child.Evaluate(clanConfig));
        }

        if (values.Count == 0) return 0f;

        float result = 0f;
        switch (combineMode)
        {
            case CombineMode.Average: result = values.Average(); break;
            case CombineMode.Multiply:
                result = 1f;
                foreach (float v in values) result *= v;
                break;
            case CombineMode.Add: result = Mathf.Clamp01(values.Sum()); break;
            case CombineMode.Max: result = values.Max(); break;
            case CombineMode.Min: result = values.Min(); break;
        }

        return responseCurve.Evaluate(result);
    }

    public override string GetDebugString(AIClanSO clanConfig)
    {
        string childrenStr = string.Join(", ", children.Select(c => c?.GetDebugString(clanConfig) ?? "null"));
        return $"{considerationName} ({combineMode}): [{childrenStr}] = {Evaluate(clanConfig):F2}";
    }
}

public enum CombineMode { Average, Multiply, Add, Max, Min }

// ========== CONCRETE CONSIDERATIONS ==========

[System.Serializable]
[AddTypeMenu("Time Elapsed")]
public class TimeElapsedConsideration : AIConsideration
{
    public override float Evaluate(AIClanSO clanConfig)
    {
        float normalized = Timer.Instance != null ? Timer.Instance.GetNormalizedElapsed() : 0f;
        return responseCurve.Evaluate(normalized);
    }

    public override string GetDebugString(AIClanSO clanConfig)
    {
        float elapsed = Timer.Instance != null ? Timer.Instance.ElapsedTimeSeconds : 0f;
        return $"{considerationName}: {elapsed:F1}s → {Evaluate(clanConfig):F2}";
    }
}

[System.Serializable]
[AddTypeMenu("Player Unit Count")]
public class PlayerUnitCountConsideration : AIConsideration
{
    public override float Evaluate(AIClanSO clanConfig)
    {
        int count = UnitTracker.Instance != null ? UnitTracker.Instance.GetTeamUnitCount("Player") : 0;
        float normalized = Mathf.Clamp01(count / clanConfig.maxUnits);
        return responseCurve.Evaluate(normalized);
    }
    // GetDebugString same pattern
}

[System.Serializable]
[AddTypeMenu("Zone Pressure")]
public class ZonePressureConsideration : AIConsideration
{
    public ZoneType targetZone = ZoneType.Upper;

    public override float Evaluate(AIClanSO clanConfig)
    {
        float dominance = UnitTracker.Instance != null ? UnitTracker.Instance.GetZoneDominance(targetZone) : 0.5f;
        float pressure = 1f - dominance;
        return responseCurve.Evaluate(pressure);
    }

    public override string GetDebugString(AIClanSO clanConfig)
    {
        float dominance = UnitTracker.Instance != null ? UnitTracker.Instance.GetZoneDominance(targetZone) : 0.5f;
        return $"{considerationName} ({targetZone}): {1f - dominance:P0} → {Evaluate(clanConfig):F2}";
    }
}

[System.Serializable]
[AddTypeMenu("Closest Enemy Distance")]
public class ClosestEnemyDistanceConsideration : AIConsideration
{
    public override float Evaluate(AIClanSO clanConfig)
    {
        float maxDistance = clanConfig.MaxDistance;
        float distance = maxDistance;

        if (UnitTracker.Instance != null && UnitTracker.Instance.EnemyBase != null)
        {
            float rawDistance = UnitTracker.Instance.GetClosestPlayerUnitDistance(UnitTracker.Instance.EnemyBase.position, out _);
            if (rawDistance < float.MaxValue) distance = rawDistance;
        }

        float normalized = Mathf.Clamp01(distance / maxDistance);
        return responseCurve.Evaluate(normalized);
    }
}

[System.Serializable]
[AddTypeMenu("Closest Enemy Power")]
public class ClosestEnemyPowerConsideration : AIConsideration
{
    public override float Evaluate(AIClanSO clanConfig)
    {
        float power = 0f;
        if (UnitTracker.Instance != null && UnitTracker.Instance.EnemyBase != null)
            UnitTracker.Instance.GetClosestPlayerUnitDistance(UnitTracker.Instance.EnemyBase.position, out power);

        float normalized = Mathf.Clamp01(power / clanConfig.maxEnemyPower);
        return responseCurve.Evaluate(normalized);
    }

    public override string GetDebugString(AIClanSO clanConfig)
    {
        return $"{considerationName} → {Evaluate(clanConfig):F2}";
    }
}

public enum ZoneType { Upper, Middle, Lower }