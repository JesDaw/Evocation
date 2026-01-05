using UnityEngine;

/// <summary>
/// Centralized manager for core game mechanics: money, timer, and spawning systems.
/// Provides a single point of control for enabling/disabling these systems.
/// Works with the existing Money, Timer, and SpawnObjects scripts.
/// </summary>
public class GameMechanicsManager : MonoBehaviour
{
    public static GameMechanicsManager Instance { get; private set; }
    
    [Header("System References")]
    [SerializeField] private Money moneySystem;
    [SerializeField] private Timer timerSystem;
    [SerializeField] private SpawnObjects playerSpawner;
    [SerializeField] private SpawnObjects enemySpawner;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Auto-find systems if not assigned
        AutoFindSystems();
    }
    
    void Start()
    {
        // Start with everything disabled
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
    
    private void AutoFindSystems()
    {
        if (moneySystem == null)
        {
            moneySystem = FindAnyObjectByType<Money>();
            if (moneySystem == null && showDebugLogs)
                Debug.LogWarning("[GameMechanicsManager] Money system not found!");
        }
        
        if (timerSystem == null)
        {
            timerSystem = FindAnyObjectByType<Timer>();
            if (timerSystem == null && showDebugLogs)
                Debug.LogWarning("[GameMechanicsManager] Timer system not found!");
        }
    }
    
    private void LogSystemReferences()
    {
        Debug.Log($"[GameMechanicsManager] Systems Found:\n" +
                  $"  Money: {(moneySystem != null ? "✓" : "✗")}\n" +
                  $"  Timer: {(timerSystem != null ? "✓" : "✗")}\n" +
                  $"  Player Spawner: {(playerSpawner != null ? "✓" : "✗")}\n" +
                  $"  Enemy Spawner: {(enemySpawner != null ? "✓" : "✗")}");
    }
    
    #region Money System
    
    public void SetMoneyActive(bool active)
    {
        if (moneySystem == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[GameMechanicsManager] Money system not found!");
            return;
        }
        
        if (active)
        {
            moneySystem.ActivateMoney();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Money activated");
        }
        else
        {
            moneySystem.DeactivateMoney();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Money deactivated");
        }
    }
    
    public void ResetMoney()
    {
        if (moneySystem != null)
        {
            moneySystem.ResetMoney();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Money reset");
        }
    }
    
    public void IncreaseMoneyGeneration()
    {
        if (moneySystem != null)
        {
            moneySystem.IncreaseMoneyGen();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Money generation increased");
        }
    }
    
    public bool IsMoneyActive()
    {
        return moneySystem != null && moneySystem.MoneyIsActive;
    }
    
    #endregion
    
    #region Timer System
    
    public void SetTimerActive(bool active)
    {
        if (timerSystem == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[GameMechanicsManager] Timer system not found!");
            return;
        }
        
        if (active)
        {
            timerSystem.ActivateTimer();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Timer activated");
        }
        else
        {
            timerSystem.DeactivateTimer();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Timer deactivated");
        }
    }
    
    public void ResetTimer()
    {
        if (timerSystem != null)
        {
            timerSystem.ResetTimer();
            if (showDebugLogs) Debug.Log("[GameMechanicsManager] Timer reset");
        }
    }
    
    public bool IsTimerActive()
    {
        return timerSystem != null && timerSystem.TimeIsActive;
    }
    
    #endregion
    
    #region Spawning Systems
    
    public void SetPlayerSpawningActive(bool active)
    {
        if (playerSpawner == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[GameMechanicsManager] Player spawner not found!");
            return;
        }
        
        playerSpawner.SpawningIsActive = active;
        if (showDebugLogs) 
            Debug.Log($"[GameMechanicsManager] Player spawning {(active ? "activated" : "deactivated")}");
    }
    
    public void SetEnemySpawningActive(bool active)
    {
        if (enemySpawner == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[GameMechanicsManager] Enemy spawner not found!");
            return;
        }
        
        enemySpawner.SpawningIsActive = active;
        if (showDebugLogs) 
            Debug.Log($"[GameMechanicsManager] Enemy spawning {(active ? "activated" : "deactivated")}");
    }
    
    public void SetPlayerSpawner(SpawnObjects spawner)
    {
        playerSpawner = spawner;
        if (showDebugLogs) Debug.Log("[GameMechanicsManager] Player spawner assigned");
    }
    
    public void SetEnemySpawner(SpawnObjects spawner)
    {
        enemySpawner = spawner;
        if (showDebugLogs) Debug.Log("[GameMechanicsManager] Enemy spawner assigned");
    }
    
    public bool IsPlayerSpawningActive()
    {
        return playerSpawner != null && playerSpawner.SpawningIsActive;
    }
    
    public bool IsEnemySpawningActive()
    {
        return enemySpawner != null && enemySpawner.SpawningIsActive;
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