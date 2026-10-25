using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scores how much an AI candidate improves its position in a zone.
/// Uses ExpectedOutcomeCalculator as the single combat model.
/// The raw outcome is positive for Player advantage, so an AI improvement is (before - after).
/// </summary>
public static class SpawnScorer
{
    /// <summary>
    /// Returns the largest positive marginal improvement across zones. Zero means no zone
    /// produces a positive improvement. bestZone identifies the zone to prioritize.
    /// </summary>
    public static float Score(ScriptableStats candidate, IEnumerable<MapZone> zones,
        float aiBaseX, float playerBaseX, out MapZone bestZone, int copies = 1)
    {
        bestZone = null;
        if (candidate == null || zones == null || copies <= 0) return 0f;

        float bestScore = 0f;
        foreach (MapZone zone in zones)
        {
            if (zone == null) continue;

            // A candidate is useful only where there is an opposing force to engage.
            bool hasThreats = HasLiveUnits(zone.PlayerUnits) || HasLiveUnits(zone.AllyUnits);
            if (!hasThreats) continue;

            float before = zone.ExpectedOutcome();
            float after = zone.ExpectedOutcome(candidate, false, copies);

            // ExpectedOutcome is positive for Player advantage and negative for AI advantage.
            float gain = before - after;
            if (gain > bestScore)
            {
                bestScore = gain;
                bestZone = zone;
            }
        }

        return bestScore;
    }

    static bool HasLiveUnits(List<GameObject> units)
    {
        if (units == null) return false;
        foreach (GameObject obj in units)
        {
            if (obj == null) continue;
            Stats stats = obj.GetComponent<Stats>();
            if (stats != null && !stats._IsDead && stats._CurrentHealth > 0f) return true;
        }
        return false;
    }
}
