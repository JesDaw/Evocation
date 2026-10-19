using System.Collections.Generic;
using UnityEngine;

// UNTESTED SKETCH. Sign convention here: positive = AI favored.
// Wiring (in ExicuteBestAction), replacing the expectedOutcome pieces:
//   cu.expectedOutcomeUtility = SpawnScorer.Score(character, UnitTracker.Instance.Zones,
//       aiBaseX, playerBaseX, out MapZone bestZone, copies) / 2f;   // gain is in [0,2]
// UnitTracker needs to expose `zones` and the base X positions
// (aiBaseX = EnemyBase.position.x, playerBaseX = PlayerBase.position.x in your naming).
// maxAbsDelta normalization is no longer needed.

/// One unit (or a fraction of one) as seen by the evaluator.
public struct Combatant
{
    public float dps;
    public float hp;
    public float weight;      // 1 = fully present; ThreatAssessor shrinks this for distant units
    public bool isAOE;
    public int maxTargets;

    public static Combatant FromScriptable(ScriptableStats s, float weight = 1f)
    {
        var p = PowerMath.GetProfile(s);
        return new Combatant
        {
            dps = PowerMath.GetDPS(p), hp = Mathf.Max(1f, p.HP), weight = weight,
            isAOE = p.MaxTargets != 1, maxTargets = p.MaxTargets
        };
    }

    public static Combatant FromLive(Stats s, float weight = 1f)
    {
        var p = PowerMath.GetProfile(s);
        return new Combatant
        {
            dps = PowerMath.GetDPS(p), hp = Mathf.Max(1f, s._CurrentHealth), weight = weight,
            isAOE = p.MaxTargets != 1, maxTargets = p.MaxTargets
        };
    }
}

/// Pure combat math. Knows nothing about maps or distance.
public static class BattleEvaluator
{
    /// Smooth margin in [-1, 1]. +1 = "mine" dominates, -1 = "theirs" dominates.
    public static float Margin(List<Combatant> mine, List<Combatant> theirs)
    {
        float mineStrength = Strength(mine, Presence(theirs));
        float theirStrength = Strength(theirs, Presence(mine));
        float total = mineStrength + theirStrength;
        return total <= 0f ? 0f : (mineStrength - theirStrength) / total;
    }

    static float Presence(List<Combatant> army)
    {
        float sum = 0f;
        foreach (var c in army) sum += c.weight;
        return sum;
    }

    // S = (sum dps) * (sum hp). For uniform armies this is N^2 * dps * hp,
    // i.e. Lanchester's square-law winner criterion.
    static float Strength(List<Combatant> army, float enemyPresence)
    {
        float dps = 0f, hp = 0f;
        foreach (var c in army)
        {
            float targets = c.isAOE ? Mathf.Clamp(enemyPresence, 1f, c.maxTargets) : 1f;
            dps += c.dps * c.weight * targets;
            hp += c.hp * c.weight;
        }
        return dps * hp;
    }
}

/// Knows positions. Knows nothing about combat math.
public static class ThreatAssessor
{
    public static float ProximityWeight(float unitX, float aiBaseX, float playerBaseX,
        float steepness = 2f, float floor = 0.05f)
    {
        float span = Mathf.Abs(playerBaseX - aiBaseX);
        if (span <= 0f) return 1f;
        float t = Mathf.Clamp01(Mathf.Abs(unitX - aiBaseX) / span); // 0 at AI base, 1 at far end
        return Mathf.Max(floor, Mathf.Pow(1f - t, steepness));
    }

    /// urgency = highest proximity weight among the zone's threats (0 if none).
    public static void BuildZoneForces(MapZone zone, float aiBaseX, float playerBaseX,
        List<Combatant> mine, List<Combatant> threats, out float urgency)
    {
        mine.Clear();
        threats.Clear();
        urgency = 0f;

        // "Enemy" tag = the AI's own units in this codebase.
        foreach (var go in zone.EnemyUnits)
        {
            var s = go != null ? go.GetComponent<Stats>() : null;
            if (s == null || s._IsDead) continue;
            mine.Add(Combatant.FromLive(s));
        }

        float maxW = 0f;
        AddThreats(zone.PlayerUnits, aiBaseX, playerBaseX, threats, ref maxW);
        AddThreats(zone.AllyUnits, aiBaseX, playerBaseX, threats, ref maxW);
        urgency = maxW;
    }

    static void AddThreats(List<GameObject> units, float aiBaseX, float playerBaseX,
        List<Combatant> threats, ref float maxW)
    {
        foreach (var go in units)
        {
            var s = go != null ? go.GetComponent<Stats>() : null;
            if (s == null || s._IsDead) continue;
            float w = ProximityWeight(go.transform.position.x, aiBaseX, playerBaseX);
            maxW = Mathf.Max(maxW, w);
            threats.Add(Combatant.FromLive(s, w));
        }
    }
}

/// Marginal value of spawning `copies` of a candidate, in the zone where it helps most.
public static class SpawnScorer
{
    // Returns a gain in [0, 2] scaled by zone urgency. 0 = no useful placement.
    public static float Score(ScriptableStats candidate, IEnumerable<MapZone> zones,
        float aiBaseX, float playerBaseX, out MapZone bestZone, int copies = 1)
    {
        bestZone = null;
        float best = 0f;

        var mine = new List<Combatant>();
        var threats = new List<Combatant>();
        Combatant template = Combatant.FromScriptable(candidate);

        foreach (var zone in zones)
        {
            if (zone == null) continue;

            ThreatAssessor.BuildZoneForces(zone, aiBaseX, playerBaseX, mine, threats, out float urgency);
            if (threats.Count == 0) continue; // nothing to fight here, nothing to gain

            float before = BattleEvaluator.Margin(mine, threats);
            for (int i = 0; i < copies; i++) mine.Add(template);
            float after = BattleEvaluator.Margin(mine, threats);

            float gain = (after - before) * urgency;
            if (gain > best)
            {
                best = gain;
                bestZone = zone;
            }
        }
        return best;
    }
}
