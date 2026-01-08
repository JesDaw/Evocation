using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Universal spawner for CPUs and Players
/// Works with both AI (AISpawnerController) and Player (SpawnController)
/// </summary>
public class SpawnObjects : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("Is this an enemy spawner? (moves right-to-left)")]
    [SerializeField] bool enemySpawner;
    
    [Tooltip("Enable/disable spawning")]
    public bool spawningEnabled = true;

    [Header("References")]
    [SerializeField] GameObject cpuPrefab;
    [SerializeField] Transform cpuContainer;
    [SerializeField] Transform playerContainer;
    [SerializeField] Transform spawnLocation;

    [Header("Player Spawning")]
    [SerializeField] FloatVariable playerMoney;
    [SerializeField] PlayerSwitch playerSwitch;
    [SerializeField] PlayerLivesManager playerLivesManager;
    [SerializeField] Money moneyDisplay;

    [Header("Events")]
    [SerializeField] UnityEvent<GameObject> onSpawn;

    // Property for compatibility with old code
    public bool SpawningIsActive
    {
        get { return spawningEnabled; }
        set { spawningEnabled = value; }
    }

    void Start()
    {
        if (moneyDisplay == null)
        {
            moneyDisplay = FindAnyObjectByType<Money>();
            if (moneyDisplay == null)
                Debug.LogError("SpawnObjects can't find Money script!");
        }

        if (spawnLocation == null)
            Debug.LogError("No spawn location set!");
    }

    /// <summary>
    /// Spawn a CPU unit (called by AI or Player)
    /// </summary>
    public GameObject SpawnCPU(ScriptableStats stats)
    {
        if (!spawningEnabled)
        {
            Debug.Log("Spawning is disabled");
            return null;
        }

        if (stats == null)
        {
            Debug.LogWarning("Tried to spawn with null stats!");
            return null;
        }

        // Instantiate the CPU
        GameObject spawnedUnit = Instantiate(
            cpuPrefab,
            spawnLocation.position,
            spawnLocation.rotation,
            cpuContainer
        );

        // Configure the CPU with stats
        Stats unitStats = spawnedUnit.GetComponent<Stats>();
        if (unitStats != null)
        {
            unitStats.scriptableStats = stats;
            unitStats._Enemy = enemySpawner;
        }
        else
        {
            Debug.LogError("Spawned unit has no Stats component!");
        }

        // Set tag based on spawner type
        string unitTag = enemySpawner ? "Enemy" : "Allies";
        spawnedUnit.tag = unitTag;

        // Set layer (keeping your original layer logic)
        int layer = enemySpawner ? 9 : 10;
        SetLayerRecursively(spawnedUnit, layer);

        // Randomize Y position slightly
        RandomizeAppearancePosition(spawnedUnit);

        // Trigger event
        onSpawn?.Invoke(spawnedUnit);

        return spawnedUnit;
    }

    /// <summary>
    /// Spawn a CPU from AI system (no money cost)
    /// </summary>
    public GameObject SpawnFromSpawner(ScriptableStats stats)
    {
        return SpawnCPU(stats);
    }

    /// <summary>
    /// Spawn a CPU from player (costs money)
    /// </summary>
    public GameObject SpawnFromPlayer(ScriptableStats stats)
    {
        if (!spawningEnabled)
        {
            Debug.Log("Spawning is disabled");
            return null;
        }

        if (playerMoney == null || stats == null)
        {
            Debug.LogWarning("Missing references for player spawn!");
            return null;
        }

        // Check money
        if (playerMoney._Value < stats._spawnCost)
        {
            Debug.Log($"Not enough money! Need {stats._spawnCost}, have {playerMoney._Value}");
            return null;
        }

        // Deduct cost
        playerMoney._Value -= stats._spawnCost;
        
        // Update display
        if (moneyDisplay != null)
            moneyDisplay.UpdateMoneyDesplay();

        // Spawn the unit
        GameObject spawnedUnit = SpawnCPU(stats);
        
        //Debug.Log($"Player spawned {stats.name} (Cost: {stats._spawnCost})");
        
        return spawnedUnit;
    }

    /// <summary>
    /// Spawn a player character
    /// </summary>
    public GameObject SpawnPlayer(GameObject playerPrefab)
    {
        if (!spawningEnabled)
        {
            Debug.Log("Spawning is disabled");
            return null;
        }

        if (playerLivesManager != null && !playerLivesManager.canSpawnMore)
        {
            Debug.Log("Cannot spawn more players!");
            return null;
        }

        // Get cost
        Stats playerStats = playerPrefab.GetComponent<Stats>();
        if (playerStats == null)
        {
            Debug.LogError("Player prefab has no Stats component!");
            return null;
        }

        int cost = playerStats._spawnCost;

        // Check money
        if (playerMoney._Value < cost)
        {
            Debug.Log($"Not enough money to spawn player! Need {cost}, have {playerMoney._Value}");
            return null;
        }

        // Deduct cost
        playerMoney._Value -= cost;

        // Spawn player
        GameObject spawnedPlayer = Instantiate(
            playerPrefab,
            spawnLocation.position,
            spawnLocation.rotation,
            playerContainer
        );

        // Set up player stats
        Stats spawnedStats = spawnedPlayer.GetComponent<Stats>();
        if (spawnedStats != null)
        {
            // Subscribe to death event
            spawnedStats.OnDeath.DynamicCalls += () => 
            {
                if (playerLivesManager != null)
                    playerLivesManager.LooseLife(spawnedPlayer);
            };
        }

        // Register with player switch
        if (playerSwitch != null)
            playerSwitch.AddPlayer(spawnedPlayer);

        // Register life
        if (playerLivesManager != null)
            playerLivesManager.GainLife();

        //Debug.Log($"Player spawned (Cost: {cost})");

        return spawnedPlayer;
    }

    /// <summary>
    /// Set layer recursively for all children
    /// </summary>
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Randomize appearance position slightly
    /// </summary>
    void RandomizeAppearancePosition(GameObject unit)
    {
        Transform appearance = unit.transform.Find("CpuAppearance");
        if (appearance == null)
            appearance = unit.transform.Find("Appearance");
        
        if (appearance != null)
        {
            float randomY = Random.Range(-0.5f, 0.5f);
            Vector3 pos = appearance.position;
            pos.y += randomY;
            pos.z += randomY; // Depth sorting
            appearance.position = pos;
        }
    }

    /// <summary>
    /// Enable/disable spawning
    /// </summary>
    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;
    }
}