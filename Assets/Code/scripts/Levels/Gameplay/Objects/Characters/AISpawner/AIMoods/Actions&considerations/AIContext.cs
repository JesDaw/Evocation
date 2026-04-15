using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AIContext
{
    [Header("Game Systems - Assign These!")]
    public AIMoneyManager aiMoneyManager;
    public Timer timer;
    
    [Header("Map Zones - Assign These!")]
    public MapZonesManager upperZone;
    public MapZonesManager middleZone;
    public MapZonesManager lowerZone;
    
    [Header("Spatial References")]
    public Transform aiBase;
    public Transform playerBase;
    public float maxDistance = 50f;
    
    [Header("Normalization Settings")]
    public float maxMoney = 100f;
    public float maxUnits = 20f;
    public float maxEnemyPower = 50f;
    public float maxActionWaitTime = 15f;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    [HideInInspector] public float lastActionTime = 0f;

    [HideInInspector] public int currentLoopCount = 0;
    private Dictionary<string, int> actionLastExecutedLoop = new Dictionary<string, int>();

    private float cachedClosestEnemyDistance;
    private float cachedClosestEnemyPower;
    private int cachedPlayerUnits;
    private int cachedAIUnits;

    public void UpdateContext()
    {
        UpdateUnitCounts();
        UpdateClosestEnemy();
    }

    #region Normalized Getters (0-1 range)
    
    public float GetNormalizedMoney() => aiMoneyManager != null ? Mathf.Clamp01(aiMoneyManager.GetMoney() / maxMoney) : 0f;
    
    public float GetNormalizedTimeElapsed() => (timer != null && timer.TimeIsActive) ? Mathf.Clamp01((timer.maxTimeRemaining - Timer.Instance.RemainingTimeSeconds) / timer.maxTimeRemaining) : 0f;
    
    public float GetNormalizedTimeRemaining() => (timer != null && timer.TimeIsActive) ? Mathf.Clamp01(Timer.Instance.RemainingTimeSeconds / timer.maxTimeRemaining) : 1f;
    
    public float GetNormalizedClosestEnemy() => Mathf.Clamp01(cachedClosestEnemyDistance / maxDistance);
    
    public float GetNormalizedPlayerUnits() => Mathf.Clamp01(cachedPlayerUnits / maxUnits);
    
    public float GetNormalizedAIUnits() => Mathf.Clamp01(cachedAIUnits / maxUnits);
    
    public float GetNormalizedClosestEnemyPower() => Mathf.Clamp01(cachedClosestEnemyPower / maxEnemyPower);
    
    public float GetNormalizedTimeSinceLastAction() => Mathf.Clamp01((Time.time - lastActionTime) / maxActionWaitTime);
    
    public float GetNormalizedUnitsInZone(ZoneType zone, string tag)
    {
        MapZonesManager manager = GetZoneManager(zone);
        if (manager == null) return 0f;
        
        int count = manager.GetCountByTag(tag);
        return Mathf.Clamp01(count / maxUnits);
    }
    
    public float GetNormalizedZoneDominance(ZoneType zone)
    {
        MapZonesManager manager = GetZoneManager(zone);
        if (manager == null) return 0.5f;
        
        return manager.GetTagRatio("Enemy", "Player");
    }
    
    #endregion

    #region Raw Value Getters
    
    public float GetCurrentMoney() => aiMoneyManager != null ? aiMoneyManager.GetMoney() : 0f;
    
    public float GetTimeElapsed() => timer != null ? timer.maxTimeRemaining - Timer.Instance.RemainingTimeSeconds : 0f;
    
    public float GetTimeRemaining() => Timer.Instance != null ? Timer.Instance.RemainingTimeSeconds : 0f;
    
    public int GetPlayerUnitCount() => cachedPlayerUnits;
    
    public int GetAIUnitCount() => cachedAIUnits;
    
    public float GetClosestEnemyDistance() => cachedClosestEnemyDistance;
    
    public float GetRawClosestEnemyPower() => cachedClosestEnemyPower;
    
    public float GetRawTimeSinceLastAction() => Time.time - lastActionTime;
    
    public int GetUnitsInZone(ZoneType zone, string tag)
    {
        MapZonesManager manager = GetZoneManager(zone);
        return manager != null ? manager.GetCountByTag(tag) : 0;
    }
    
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
        {
            if (showDebugLogs) Debug.LogWarning("[AIContext] aiBase is null!");
            return;
        }
        
        if (showDebugLogs) Debug.Log($"[AIContext] aiBase position: {aiBase.position}");
        
        CheckClosestInZone(upperZone);
        CheckClosestInZone(middleZone);
        CheckClosestInZone(lowerZone);
        
        if (showDebugLogs) Debug.Log($"[AIContext] Final closest enemy distance: {cachedClosestEnemyDistance}");
    }
    
    private void CheckClosestInZone(MapZonesManager zone)
    {
        if (zone == null) return;
        
        List<GameObject> playerUnits = zone.GetObjectsByTag("Player");
        List<GameObject> allyUnits = zone.GetObjectsByTag("Allies");
        
        if (showDebugLogs)
        {
            Debug.Log($"[AIContext] Zone {zone.name}: Player={playerUnits.Count}, Allies={allyUnits.Count}");
        }
        
        CheckUnitsInList(playerUnits);
        CheckUnitsInList(allyUnits);
    }
    
    private void CheckUnitsInList(List<GameObject> units)
    {
        foreach (GameObject unit in units)
        {
            if (unit == null)
            {
                if (showDebugLogs) Debug.Log($"[AIContext] Skipping null unit");
                continue;
            }
            
            float distance = Vector3.Distance(unit.transform.position, aiBase.position);
            
            if (showDebugLogs)
            {
                Debug.Log($"[AIContext] Checking {unit.name} at distance {distance:F1} from aiBase");
            }
            
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
            case ZoneType.All: return null;
            default: return null;
        }
    }
    
    #endregion

    #region Action Tracking
    
    public void RecordActionExecuted(string actionName)
    {
        actionLastExecutedLoop[actionName] = currentLoopCount;
    }
    
    public int GetLoopsSinceLastPicked(string actionName)
    {
        if (!actionLastExecutedLoop.ContainsKey(actionName))
            return currentLoopCount;
        return currentLoopCount - actionLastExecutedLoop[actionName];
    }
    
    public float GetLoopsSinceLastPickedNormalized(string actionName, int maxLoops = 10)
    {
        return Mathf.Clamp01((float)GetLoopsSinceLastPicked(actionName) / maxLoops);
    }
    
    #endregion
}
