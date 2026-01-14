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
    [SerializeField] bool DebugLogs = false;

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

        Stats unitStats = spawnedUnit.GetComponent<Stats>();
        if (unitStats != null)
        {
            unitStats.scriptableStats = stats;
            unitStats._Enemy = enemySpawner;
            unitStats.InitializeStats();
        }
        else
        {
            Debug.LogError("Spawned unit has no Stats component!");
        }

        string unitTag = enemySpawner ? "Enemy" : "Allies";
        spawnedUnit.tag = unitTag;

        int layer = enemySpawner ? 9 : 10;
        SetLayerRecursively(spawnedUnit, layer);

        RandomizeAppearancePosition(spawnedUnit);

        onSpawn?.Invoke(spawnedUnit);

        return spawnedUnit;
    }

    public GameObject SpawnFromSpawner(ScriptableStats stats)
    {
        return SpawnCPU(stats);
    }

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

        if (playerMoney._Value < stats._spawnCost)
        {
            if (DebugLogs) Debug.Log($"Not enough money! Need {stats._spawnCost}, have {playerMoney._Value}");
            return null;
        }

        playerMoney._Value -= stats._spawnCost;
        
        if (moneyDisplay != null)
            moneyDisplay.UpdateMoneyDesplay();

        GameObject spawnedUnit = SpawnCPU(stats);
        
        if(DebugLogs)Debug.Log($"Player spawned {stats.name} (Cost: {stats._spawnCost})");
        
        return spawnedUnit;
    }

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

        Stats playerStats = playerPrefab.GetComponent<Stats>();
        if (playerStats == null)
        {
            Debug.LogError("Player prefab has no Stats component!");
            return null;
        }

        int cost = playerStats._spawnCost;

        if (playerMoney._Value < cost)
        {
            Debug.Log($"Not enough money to spawn player! Need {cost}, have {playerMoney._Value}");
            return null;
        }

        playerMoney._Value -= cost;

        GameObject spawnedPlayer = Instantiate(
            playerPrefab,
            spawnLocation.position,
            spawnLocation.rotation,
            playerContainer
        );

        Stats spawnedStats = spawnedPlayer.GetComponent<Stats>();
        if (spawnedStats != null)
        {
            spawnedStats.OnDeath.DynamicCalls += () => 
            {
                if (playerLivesManager != null)
                    playerLivesManager.LooseLife(spawnedPlayer);
            };
        }

        if (playerSwitch != null)
            playerSwitch.AddPlayer(spawnedPlayer);

        if (playerLivesManager != null)
            playerLivesManager.GainLife();

        if (DebugLogs) Debug.Log($"Player spawned (Cost: {cost})");

        return spawnedPlayer;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        /*foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }*/
    }

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
            pos.z += randomY; 
            appearance.position = pos;
        }
    }

    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;
    }
}