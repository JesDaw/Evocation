
using System;
using System.Collections.Generic;

/// <summary>
/// Immutable plan specification passed from selection to execution.
/// Does not depend on AIClan, AIStrategicDirector or MonoBehaviour.
/// No runtime state or Unity asset references are modified.
/// </summary>
public sealed class AITacticalPlan
{
    readonly ScriptableStats[] units;
    readonly IReadOnlyList<ScriptableStats> readOnlyUnits;

    public IReadOnlyList<ScriptableStats> Units => readOnlyUnits;
    public string ObjectiveLabel { get; private set; }

    public AITacticalPlan(
        IEnumerable<ScriptableStats> orderedUnits,
        string objectiveLabel)
    {
        if (orderedUnits == null)
            throw new ArgumentNullException(nameof(orderedUnits));

        var copied = new List<ScriptableStats>();

        foreach (ScriptableStats unit in orderedUnits)
        {
            if (unit == null)
                throw new ArgumentException(
                    "A tactical plan cannot contain a null unit.",
                    nameof(orderedUnits));

            copied.Add(unit);
        }

        if (copied.Count == 0)
            throw new ArgumentException(
                "A tactical plan needs at least one unit.",
                nameof(orderedUnits));

        units = copied.ToArray();
        readOnlyUnits = Array.AsReadOnly(units);
        ObjectiveLabel = objectiveLabel ?? string.Empty;
    }
}
