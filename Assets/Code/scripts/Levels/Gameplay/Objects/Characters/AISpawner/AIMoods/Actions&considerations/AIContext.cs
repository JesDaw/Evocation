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
    
    [Header("Map Zones")]
    public MapZonesManager upperZone;
    public MapZonesManager middleZone;
    public MapZonesManager lowerZone;
    
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
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
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
        MapZonesManager manager = GetZoneManager(zone);
        if (manager == null)
            return 0.5f;
        
        return manager.GetTagRatio("Enemy", "Player");
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
        
        if (upperZone != null)
        {
            cachedPlayerUnits += upperZone.GetCountByTag("Player") + upperZone.GetCountByTag("Allies");
            cachedAIUnits += upperZone.GetCountByTag("Enemy");
        }
        if (middleZone != null)
        {
            cachedPlayerUnits += middleZone.GetCountByTag("Player") + middleZone.GetCountByTag("Allies");
            cachedAIUnits += middleZone.GetCountByTag("Enemy");
        }
        if (lowerZone != null)
        {
            cachedPlayerUnits += lowerZone.GetCountByTag("Player") + lowerZone.GetCountByTag("Allies");
            cachedAIUnits += lowerZone.GetCountByTag("Enemy");
        }
    }
    
    private void UpdateClosestEnemy()
    {
        cachedClosestEnemyDistance = maxDistance;
        cachedClosestEnemyPower = 0f;
        
        if (aiBase == null)
            return;
        
        CheckClosestInZone(upperZone);
        CheckClosestInZone(middleZone);
        CheckClosestInZone(lowerZone);
    }
    
    private void CheckClosestInZone(MapZonesManager zone)
    {
        if (zone == null)
            return;
        
        List<GameObject> playerUnits = zone.GetObjectsByTag("Player");
        List<GameObject> allyUnits = zone.GetObjectsByTag("Allies");
        
        CheckUnitsInList(playerUnits);
        CheckUnitsInList(allyUnits);
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
    
    private MapZonesManager GetZoneManager(ZoneType zone)
    {
        switch (zone)
        {
            case ZoneType.Upper: return upperZone;
            case ZoneType.Middle: return middleZone;
            case ZoneType.Lower: return lowerZone;
            default: return null;
        }
    }
    
    #endregion
}