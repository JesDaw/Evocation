using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Context data passed to AI for decision making
/// Now properly reads from actual game systems!
/// </summary>
[System.Serializable]
public class AIContext
{
    [Header("Game Systems - Assign These!")]
    public SpawnObjects spawner; // REQUIRED for spawning
    public AIMoneyManager aiMoneyManager; // REQUIRED - AI uses AIMoneyManager, NOT Money!
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
    
    /// <summary>
    /// Get money as 0-1 (0 = no money, 1 = max money)
    /// </summary>
    public float GetNormalizedMoney()
    {
        if (aiMoneyManager == null)
        {
            Debug.LogWarning("AIContext: aiMoneyManager is null!");
            return 0f;
        }
        
        float current = aiMoneyManager.GetMoney();
        float max = 100; 
        
        float normalized = Mathf.Clamp01(current / max);
        
        if (Application.isEditor)
            Debug.Log($"      Money: current={current:F1}, max={max:F1}, normalized={normalized:F2}");
        
        return normalized;
    }
    
    /// <summary>
    /// Get time elapsed as 0-1 (0 = start, 1 = time up)
    /// </summary>
    public float GetNormalizedTimeElapsed()
    {
        if (timer == null || !timer.TimeIsActive) return 0f;
        
        float elapsed = timer.maxTimeRemaining - timer.remainingTimeSeconds._Value;
        return Mathf.Clamp01(elapsed / timer.maxTimeRemaining);
    }
    
    /// <summary>
    /// Get time remaining as 0-1 (0 = time up, 1 = full time)
    /// </summary>
    public float GetNormalizedTimeRemaining()
    {
        if (timer == null || !timer.TimeIsActive) return 1f;
        
        return Mathf.Clamp01(timer.remainingTimeSeconds._Value / timer.maxTimeRemaining);
    }
    
    /// <summary>
    /// Get closest enemy distance as 0-1 (0 = at base, 1 = far away)
    /// </summary>
    public float GetNormalizedClosestEnemy()
    {
        return Mathf.Clamp01(cachedClosestEnemyDistance / maxDistance);
    }
    
    /// <summary>
    /// Get player unit count as 0-1 (normalized to reasonable max)
    /// </summary>
    public float GetNormalizedPlayerUnits()
    {
        float reasonableMax = 20f; // Adjust based on your game
        return Mathf.Clamp01(cachedPlayerUnits / reasonableMax);
    }
    
    /// <summary>
    /// Get AI unit count as 0-1 (normalized to reasonable max)
    /// </summary>
    public float GetNormalizedAIUnits()
    {
        float reasonableMax = 20f; // Adjust based on your game
        return Mathf.Clamp01(cachedAIUnits / reasonableMax);
    }
    
    /// <summary>
    /// Get unit count in specific zone as 0-1
    /// </summary>
    public float GetNormalizedUnitsInZone(ZoneType zone, string tag)
    {
        string key = $"{zone}_{tag}";
        if (!zoneUnitCounts.ContainsKey(key)) return 0f;
        
        float reasonableMax = 10f; // Max units we'd expect in one zone
        return Mathf.Clamp01(zoneUnitCounts[key] / reasonableMax);
    }
    
    /// <summary>
    /// Get zone dominance: 1 = AI winning, 0 = Player winning, 0.5 = contested
    /// </summary>
    public float GetZoneDominance(ZoneType zone)
    {
        MapZonesManager targetZone = GetZoneManager(zone);
        if (targetZone == null) return 0.5f;
        
        // Compare player vs enemy units in zone
        string playerKey = $"{zone}_Player";
        string allyKey = $"{zone}_Allies";
        string enemyKey = $"{zone}_Enemy";
        
        int playerUnits = zoneUnitCounts.ContainsKey(playerKey) ? zoneUnitCounts[playerKey] : 0;
        int allyUnits = zoneUnitCounts.ContainsKey(allyKey) ? zoneUnitCounts[allyKey] : 0;
        int enemyUnits = zoneUnitCounts.ContainsKey(enemyKey) ? zoneUnitCounts[enemyKey] : 0;
        
        int totalPlayer = playerUnits + allyUnits;
        int totalUnits = totalPlayer + enemyUnits;
        
        if (totalUnits == 0) return 0.5f; // Contested/empty
        
        // Return AI dominance (enemy units / total)
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
    
    private void UpdateUnitCounts()
    {
        // Use FindGameObjectsWithTag as fallback
        GameObject[] playerUnits = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] allyUnits = GameObject.FindGameObjectsWithTag("Allies");
        GameObject[] aiUnits = GameObject.FindGameObjectsWithTag("Enemy");
        
        cachedPlayerUnits = playerUnits.Length + allyUnits.Length;
        cachedAIUnits = aiUnits.Length;
    }
    
    private void UpdateClosestEnemy()
    {
        cachedClosestEnemyDistance = maxDistance;
        
        if (aiBase == null) return;
        
        GameObject[] playerUnits = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] allyUnits = GameObject.FindGameObjectsWithTag("Allies");
        
        foreach (GameObject unit in playerUnits)
        {
            if (unit == null) continue;
            float distance = Vector3.Distance(unit.transform.position, aiBase.position);
            if (distance < cachedClosestEnemyDistance)
                cachedClosestEnemyDistance = distance;
        }
        
        foreach (GameObject unit in allyUnits)
        {
            if (unit == null) continue;
            float distance = Vector3.Distance(unit.transform.position, aiBase.position);
            if (distance < cachedClosestEnemyDistance)
                cachedClosestEnemyDistance = distance;
        }
    }
    
    private void CacheZoneData()
    {
        zoneUnitCounts.Clear();
        
        // Cache counts for each zone using MapZonesManager
        CacheZoneCounts(upperZone, ZoneType.Upper);
        CacheZoneCounts(middleZone, ZoneType.Middle);
        CacheZoneCounts(lowerZone, ZoneType.Lower);
    }
    
    private void CacheZoneCounts(MapZonesManager zone, ZoneType zoneType)
    {
        if (zone == null) return;
        
        // Get counts for each tag from the zone's tracking system
        string[] tagsToTrack = { "Player", "Allies", "Enemy" };
        
        foreach (string tag in tagsToTrack)
        {
            // Use the zone's tracking to count units with this tag
            int count = CountUnitsInZone(zone, tag);
            string key = $"{zoneType}_{tag}";
            zoneUnitCounts[key] = count;
        }
    }
    
    private int CountUnitsInZone(MapZonesManager zone, string tag)
    {
        // If MapZonesManager has GetCountByTag method (from the improved version)
        // Use: return zone.GetCountByTag(tag);
        
        // Otherwise, use Physics2D as fallback
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
            if (hit.CompareTag(tag))
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