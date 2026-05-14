using UnityEngine;

/// <summary>
/// Centralized manager for core game mechanics: money, timer, and spawning systems.
/// Provides a single point of control for enabling/disabling these systems.
/// Works with the existing Money, Timer, and SpawnObjects scripts.
/// </summary>
public class GameMechanicsManager : MonoBehaviour
{
    public static GameMechanicsManager Instance { get; private set; }
        
    [Header("Debug")]
    [SerializeField] bool showDebugLogs = false;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (Money.Instance == null) Debug.LogWarning("[GameMechanicsManager] Money system not found!");
        if (Timer.Instance == null) Debug.LogWarning("[GameMechanicsManager] Timer system not found");
    }
    
    void Start()
    {
        DisableAllSystems();
        
        if (showDebugLogs)
        {
            LogSystemReferences();
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void LogSystemReferences()
    {
        Debug.Log($"[GameMechanicsManager] Systems Found:\n" +
                  $"  Money: {(Money.Instance != null ? "✓" : "✗")}\n" +
                  $"  Timer: {(Timer.Instance != null ? "✓" : "✗")}\n" +
                  $"  Player Spawner: {(SpawnObjects.PlayerInstance != null ? "✓" : "✗")}\n" +
                  $"  Enemy Spawner: {(SpawnObjects.EnemyInstance != null ? "✓" : "✗")}");
    }
    
    #region Money System
    
    public void SetMoneyActive(bool active)
    {
        if (Money.Instance == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[GameMechanicsManager] Money system not found!");
            return;
        }
        
        if (active)
        {
            Money.Instance.ActivateMoney();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Money activated");
        }
        else
        {
            Money.Instance.DeactivateMoney();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Money deactivated");
        }
    }
    
    public void ResetMoney()
    {
        if (Money.Instance != null)
        {
            Money.Instance.ResetMoney();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Money reset");
        }
    }
    
    public bool IsMoneyActive()
    {
        return Money.Instance != null && Money.Instance.MoneyIsActive;
    }
    
    #endregion
    
    #region Timer System
    
    public void SetTimerActive(bool active)
    {
        if (Timer.Instance == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[GameMechanicsManager] Timer system not found!");
            return;
        }
        
        if (active)
        {
            Timer.Instance.ActivateTimer();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Timer activated");
        }
        else
        {
            Timer.Instance.DeactivateTimer();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Timer deactivated");
        }
    }
    
    public void ResetTimer()
    {
        if (Timer.Instance != null)
        {
            Timer.Instance.ResetTimer();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Timer reset");
        }
    }
    
    public bool IsTimerActive()
    {
        return Timer.Instance != null && Timer.Instance.TimeIsActive;
    }
    
    #endregion
    
    #region Spawning Systems
    
    public void SetPlayerSpawningActive(bool active)
    {
        if (SpawnObjects.PlayerInstance == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[GameMechanicsManager] Player spawner not found!");
            return;
        }
        
        SpawnObjects.PlayerInstance.SpawningIsActive = active;
        if (showDebugLogs) 
            Debug.Log($"[GameMechanicsManager] Player spawning {(active ? "activated" : "deactivated")}");
    }
    
    public void SetEnemySpawningActive(bool active)
    {
        if (SpawnObjects.EnemyInstance == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[GameMechanicsManager] Enemy spawner not found!");
            return;
        }
        
        SpawnObjects.EnemyInstance.SpawningIsActive = active;
        if (showDebugLogs) 
            Debug.Log($"[GameMechanicsManager] Enemy spawning {(active ? "activated" : "deactivated")}");
    }
    public bool IsPlayerSpawningActive()
    {
        return SpawnObjects.PlayerInstance != null && SpawnObjects.PlayerInstance.SpawningIsActive;
    }
    
    public bool IsEnemySpawningActive()
    {
        return SpawnObjects.EnemyInstance != null && SpawnObjects.EnemyInstance.SpawningIsActive;
    }
    
    #endregion
    
    #region Utility
    
    /// <summary>
    /// Disable all game systems (useful for transitions)
    /// </summary>
    public void DisableAllSystems()
    {
        SetMoneyActive(false);
        SetTimerActive(false);
        SetPlayerSpawningActive(false);
        SetEnemySpawningActive(false);
        
        if (showDebugLogs) Debug.Log("[GameMechanicsManager] All systems disabled");
    }
    
    /// <summary>
    /// Enable gameplay systems (money, timer, spawning)
    /// </summary>
    public void EnableGameplaySystems()
    {
        SetMoneyActive(true);
        SetTimerActive(true);
        SetPlayerSpawningActive(true);
        SetEnemySpawningActive(true);
        
        if (showDebugLogs) Debug.Log("[GameMechanicsManager] All gameplay systems enabled");
    }
    
    /// <summary>
    /// Reset all systems to initial state
    /// </summary>
    public void ResetAllSystems()
    {
        ResetMoney();
        ResetTimer();
        DisableAllSystems();
        
        if (showDebugLogs) Debug.Log("[GameMechanicsManager] All systems reset");
    }
    
    /// <summary>
    /// Get status of all systems (for debugging)
    /// </summary>
    public string GetSystemStatus()
    {
        return $"[GameMechanicsManager] System Status:\n" +
               $"  Money Active: {IsMoneyActive()}\n" +
               $"  Timer Active: {IsTimerActive()}\n" +
               $"  Player Spawning: {IsPlayerSpawningActive()}\n" +
               $"  Enemy Spawning: {IsEnemySpawningActive()}";
    }
    
    /// <summary>
    /// Log current system status
    /// </summary>
    public void LogSystemStatus()
    {
        Debug.Log(GetSystemStatus());
    }
    
    #endregion
}