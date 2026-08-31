using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Context containing game state information for AI decisions
/// </summary>
[System.Serializable]
public class AIContext
{
    [Header("Game Systems")]
    public Timer timer;

    [Header("Spatial References")]
    public Transform aiBase;
    public Transform playerBase;

    [Header("Normalization Settings")]
    [Tooltip("Max distance for normalization")]
    public float maxDistance = 50f;

    [Tooltip("Max unit count for normalization")]
    public float maxUnits = 20f;

    [Tooltip("Max enemy power for normalization")]
    public float maxEnemyPower = 50f;

    // Cached values
    private float cachedClosestEnemyDistance;
    private float cachedClosestEnemyPower;
    private int cachedPlayerUnits;
    private int cachedAIUnits;

    /// <summary>
    /// Update all cached values from current game state
    /// </summary>
    public void UpdateContext()
    {
        UpdateUnitCounts();
        UpdateClosestEnemy();
    }

    #region Normalized Getters (0-1 range)

    public float GetNormalizedTimeElapsed()
    {
        if (timer == null || !timer.TimeIsActive)
            return 0f;

        float elapsed = timer.maxTimeRemaining - Timer.Instance.RemainingTimeSeconds;
        return Mathf.Clamp01(elapsed / timer.maxTimeRemaining);
    }

    public float GetNormalizedTimeRemaining()
    {
        if (timer == null || !timer.TimeIsActive)
            return 1f;

        return Mathf.Clamp01(Timer.Instance.RemainingTimeSeconds / timer.maxTimeRemaining);
    }

    public float GetNormalizedClosestEnemy()
    {
        return Mathf.Clamp01(cachedClosestEnemyDistance / maxDistance);
    }

    public float GetNormalizedPlayerUnits()
    {
        return Mathf.Clamp01(cachedPlayerUnits / maxUnits);
    }

    public float GetNormalizedAIUnits()
    {
        return Mathf.Clamp01(cachedAIUnits / maxUnits);
    }

    public float GetNormalizedClosestEnemyPower()
    {
        return Mathf.Clamp01(cachedClosestEnemyPower / maxEnemyPower);
    }

    public float GetNormalizedZoneDominance(ZoneType zone)
    {
        if (UnitTracker.Instance == null)
            return 0.5f;

        string lane = LaneNameFor(zone);
        int enemyCount = UnitTracker.Instance.FindAllUnitsWithLayer($"Enemy/{lane}").Count;
        int playerCount = UnitTracker.Instance.FindAllUnitsWithLayer($"Player/{lane}").Count;
        int total = enemyCount + playerCount;

        if (total == 0)
            return 0.5f;

        return (float)enemyCount / total;
    }

    #endregion

    #region Raw Value Getters

    public float GetTimeElapsed()
    {
        if (timer == null)
            return 0f;
        return timer.maxTimeRemaining - Timer.Instance.RemainingTimeSeconds;
    }

    public float GetTimeRemaining()
    {
        return Timer.Instance != null ? Timer.Instance.RemainingTimeSeconds : 0f;
    }

    public int GetPlayerUnitCount() => cachedPlayerUnits;

    public int GetAIUnitCount() => cachedAIUnits;

    public float GetClosestEnemyDistance() => cachedClosestEnemyDistance;

    public float GetRawClosestEnemyPower() => cachedClosestEnemyPower;

    #endregion

    #region Private Update Methods

    private void UpdateUnitCounts()
    {
        cachedPlayerUnits = 0;
        cachedAIUnits = 0;

        if (UnitTracker.Instance == null)
            return;

        CountUnitsInZone(ZoneType.Upper);
        CountUnitsInZone(ZoneType.Middle);
        CountUnitsInZone(ZoneType.Lower);
    }

    private void CountUnitsInZone(ZoneType zone)
    {
        string lane = LaneNameFor(zone);
        cachedPlayerUnits += UnitTracker.Instance.FindAllUnitsWithLayer($"Player/{lane}").Count;
        cachedAIUnits += UnitTracker.Instance.FindAllUnitsWithLayer($"Enemy/{lane}").Count;
    }

    private void UpdateClosestEnemy()
    {
        cachedClosestEnemyDistance = maxDistance;
        cachedClosestEnemyPower = 0f;

        if (aiBase == null || UnitTracker.Instance == null)
            return;

        CheckClosestInZone(ZoneType.Upper);
        CheckClosestInZone(ZoneType.Middle);
        CheckClosestInZone(ZoneType.Lower);
    }

    private void CheckClosestInZone(ZoneType zone)
    {
        string lane = LaneNameFor(zone);
        // "Player/{lane}" already includes both spawned troops and live
        // PlayerSwitch characters (see UnitTracker.FindAllUnitsWithLayer),
        // so there's no separate "Allies" list to check anymore.
        List<GameObject> playerUnits = UnitTracker.Instance.FindAllUnitsWithLayer($"Player/{lane}");
        CheckUnitsInList(playerUnits);
    }

    private void CheckUnitsInList(List<GameObject> units)
    {
        foreach (GameObject unit in units)
        {
            if (unit == null)
                continue;

            float distance = Vector3.Distance(unit.transform.position, aiBase.position);

            if (distance < cachedClosestEnemyDistance)
            {
                cachedClosestEnemyDistance = distance;

                Stats unitStats = unit.GetComponent<Stats>();
                if (unitStats != null && unitStats.scriptableStats != null)
                {
                    cachedClosestEnemyPower = unitStats.scriptableStats._CalculatedPower;
                }
            }
        }
    }

    /// <summary>
    /// Maps the old zone concept onto the lane names used by UnitTracker's layers.
    /// Adjust this mapping if Upper/Middle/Lower don't correspond to Top/Mid/Bot for your map.
    /// </summary>
    private string LaneNameFor(ZoneType zone)
    {
        switch (zone)
        {
            case ZoneType.Upper: return "TopLane";
            case ZoneType.Middle: return "MidLane";
            case ZoneType.Lower: return "BotLane";
            default: return "MidLane";
        }
    }

    #endregion
}