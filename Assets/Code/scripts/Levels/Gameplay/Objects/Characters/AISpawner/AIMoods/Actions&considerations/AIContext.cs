using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Context data passed to AI for decision making
/// FIXED: Only counts root-level units, not child objects
/// </summary>
[System.Serializable]
public class AIContext
{
    [Header("Game Systems - Assign These!")]
    public SpawnObjects spawner;
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
    
    // Cached values (updated each frame)
    private float cachedClosestEnemyDistance;
    private int cachedPlayerUnits;
    private int cachedAIUnits;
    private Dictionary<string, int> zoneUnitCounts = new Dictionary<string, int>();

    /// <summary>
    /// Update all cached values from game systems
    /// Call this at the start of each AI decision
    /// </summary>
    public void UpdateContext()
    {
        UpdateUnitCounts();
        UpdateClosestEnemy();
        CacheZoneData();
    }

    #region Normalized Getters (0-1 range)
    
    public float GetNormalizedMoney()
    {
        if (aiMoneyManager == null)
        {
            Debug.LogWarning("AIContext: aiMoneyManager is null!");
            return 0f;
        }
        
        float current = aiMoneyManager.GetMoney();
        float max = 100; 
        
        return Mathf.Clamp01(current / max);
    }
    
    public float GetNormalizedTimeElapsed()
    {
        if (timer == null || !timer.TimeIsActive) return 0f;
        
        float elapsed = timer.maxTimeRemaining - timer.remainingTimeSeconds._Value;
        return Mathf.Clamp01(elapsed / timer.maxTimeRemaining);
    }
    
    public float GetNormalizedTimeRemaining()
    {
        if (timer == null || !timer.TimeIsActive) return 1f;
        
        return Mathf.Clamp01(timer.remainingTimeSeconds._Value / timer.maxTimeRemaining);
    }
    
    public float GetNormalizedClosestEnemy()
    {
        return Mathf.Clamp01(cachedClosestEnemyDistance / maxDistance);
    }
    
    public float GetNormalizedPlayerUnits()
    {
        float reasonableMax = 20f;
        return Mathf.Clamp01(cachedPlayerUnits / reasonableMax);
    }
    
    public float GetNormalizedAIUnits()
    {
        float reasonableMax = 20f;
        return Mathf.Clamp01(cachedAIUnits / reasonableMax);
    }
    
    public float GetNormalizedUnitsInZone(ZoneType zone, string tag)
    {
        string key = $"{zone}_{tag}";
        if (!zoneUnitCounts.ContainsKey(key)) return 0f;
        
        float reasonableMax = 10f;
        return Mathf.Clamp01(zoneUnitCounts[key] / reasonableMax);
    }
    
    public float GetZoneDominance(ZoneType zone)
    {
        MapZonesManager targetZone = GetZoneManager(zone);
        if (targetZone == null) return 0.5f;
        
        string playerKey = $"{zone}_Player";
        string allyKey = $"{zone}_Allies";
        string enemyKey = $"{zone}_Enemy";
        
        int playerUnits = zoneUnitCounts.ContainsKey(playerKey) ? zoneUnitCounts[playerKey] : 0;
        int allyUnits = zoneUnitCounts.ContainsKey(allyKey) ? zoneUnitCounts[allyKey] : 0;
        int enemyUnits = zoneUnitCounts.ContainsKey(enemyKey) ? zoneUnitCounts[enemyKey] : 0;
        
        int totalPlayer = playerUnits + allyUnits;
        int totalUnits = totalPlayer + enemyUnits;
        
        if (totalUnits == 0) return 0.5f;
        
        return (float)enemyUnits / totalUnits;
    }
    
    #endregion

    #region Raw Value Getters
    
    public float GetCurrentMoney()
    {
        return aiMoneyManager != null ? aiMoneyManager.GetMoney() : 0f;
    }
    
    public float GetTimeElapsed()
    {
        if (timer == null) return 0f;
        return timer.maxTimeRemaining - timer.remainingTimeSeconds._Value;
    }
    
    public float GetTimeRemaining()
    {
        return timer != null ? timer.remainingTimeSeconds._Value : 0f;
    }
    
    public int GetPlayerUnitCount()
    {
        return cachedPlayerUnits;
    }
    
    public int GetAIUnitCount()
    {
        return cachedAIUnits;
    }
    
    public float GetClosestEnemyDistance()
    {
        return cachedClosestEnemyDistance;
    }
    
    #endregion

    #region Private Update Methods
    
    /// <summary>
    /// FIXED: Only count root-level GameObjects with tags
    /// Ignores child objects to prevent duplicate counting
    /// </summary>
    private void UpdateUnitCounts()
    {
        // Get all objects with tags
        GameObject[] playerObjs = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] allyObjs = GameObject.FindGameObjectsWithTag("Allies");
        GameObject[] aiObjs = GameObject.FindGameObjectsWithTag("Enemy");
        
        // Only count root-level objects (no parent)
        cachedPlayerUnits = CountRootObjects(playerObjs) + CountRootObjects(allyObjs);
        cachedAIUnits = CountRootObjects(aiObjs);
    }
    
    /// <summary>
    /// Count only root-level GameObjects (objects with no parent)
    /// </summary>
    private int CountRootObjects(GameObject[] objects)
    {
        int count = 0;
        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.transform.parent == null)
            {
                count++;
            }
        }
        return count;
    }
    
    private void UpdateClosestEnemy()
    {
        cachedClosestEnemyDistance = maxDistance;
        
        if (aiBase == null) return;
        
        GameObject[] playerUnits = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] allyUnits = GameObject.FindGameObjectsWithTag("Allies");
        
        foreach (GameObject unit in playerUnits)
        {
            if (unit == null || unit.transform.parent != null) continue; // Skip child objects
            float distance = Vector3.Distance(unit.transform.position, aiBase.position);
            if (distance < cachedClosestEnemyDistance)
                cachedClosestEnemyDistance = distance;
        }
        
        foreach (GameObject unit in allyUnits)
        {
            if (unit == null || unit.transform.parent != null) continue; // Skip child objects
            float distance = Vector3.Distance(unit.transform.position, aiBase.position);
            if (distance < cachedClosestEnemyDistance)
                cachedClosestEnemyDistance = distance;
        }
    }
    
    private void CacheZoneData()
    {
        zoneUnitCounts.Clear();
        
        CacheZoneCounts(upperZone, ZoneType.Upper);
        CacheZoneCounts(middleZone, ZoneType.Middle);
        CacheZoneCounts(lowerZone, ZoneType.Lower);
    }
    
    private void CacheZoneCounts(MapZonesManager zone, ZoneType zoneType)
    {
        if (zone == null) return;
        
        string[] tagsToTrack = { "Player", "Allies", "Enemy" };
        
        foreach (string tag in tagsToTrack)
        {
            int count = CountUnitsInZone(zone, tag);
            string key = $"{zoneType}_{tag}";
            zoneUnitCounts[key] = count;
        }
    }
    
    private int CountUnitsInZone(MapZonesManager zone, string tag)
    {
        Collider2D zoneCollider = zone.GetComponent<Collider2D>();
        if (zoneCollider == null) return 0;
        
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            zoneCollider.bounds.center,
            zoneCollider.bounds.size,
            0f
        );
        
        int count = 0;
        foreach (Collider2D hit in hits)
        {
            // Only count root objects
            if (hit.CompareTag(tag) && hit.transform.parent == null)
                count++;
        }
        
        return count;
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