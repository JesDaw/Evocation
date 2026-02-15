using System.Collections;
using System.Data.SqlTypes;
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

    [Header("Events")]
    [SerializeField] UnityEvent<GameObject> onSpawn;
    [SerializeField] bool DebugLogs = false;
    public static SpawnObjects EnemyInstance { get; private set; }
    public static SpawnObjects PlayerInstance { get; private set; }

    const string CHARACTER_MID_LAYER = "Character/MidLane";

    public bool SpawningIsActive
    {
        get { return spawningEnabled; }
        set { spawningEnabled = value; }
    }

    void Awake()
    {
        if (enemySpawner)
        {
            if (EnemyInstance != null && EnemyInstance != this)
            {
                Destroy(gameObject);
                return;
            }
            EnemyInstance = this;
        }
        else
        {
            if (PlayerInstance != null && PlayerInstance != this)
            {
                Destroy(gameObject);
                return;
            }
            PlayerInstance = this;
        }


    }

    void Start()
    {
        if (Money.Instance == null)
        {
            if (Money.Instance == null)
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

        // Set tag based on spawner type
        string unitTag = enemySpawner ? "Enemy" : "Allies";
        spawnedUnit.tag = unitTag;

        // Set layer to Character/MidLane (default spawn lane)
        SetCharacterLayer(spawnedUnit);

        RandomizeAppearancePosition(spawnedUnit);

        onSpawn?.Invoke(spawnedUnit);

        if (DebugLogs) Debug.Log($"Spawned {unitTag} on layer: {LayerMask.LayerToName(spawnedUnit.layer)}");

        return spawnedUnit;
    }

    /// <summary>
    /// Sets the character's layer to Character/MidLane
    /// </summary>
    private void SetCharacterLayer(GameObject unit)
    {
        string layerName = CHARACTER_MID_LAYER; 
        
        int layer = LayerMask.NameToLayer(layerName);
        
        if (layer == -1)
        {
            Debug.LogError($"Layer '{layerName}' not found! Make sure it exists in your project settings.");
            return;
        }

        unit.layer = layer;

        if (DebugLogs) Debug.Log($"Set {unit.name} to layer {layerName} (#{layer})");
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

        if (Money.Instance == null || stats == null)
        {
            Debug.LogWarning("Missing references for player spawn!");
            return null;
        }

        if (Money.Instance.CurrentMoney < stats._spawnCost)
        {
            if (DebugLogs) Debug.Log($"Not enough money! Need {stats._spawnCost}, have {Money.Instance.CurrentMoney}");
            return null;
        }

        Money.Instance.CurrentMoney -= stats._spawnCost;
        
        if (Money.Instance != null)
            Money.Instance.UpdateMoneyDesplay();

        GameObject spawnedUnit = SpawnCPU(stats);
        
        if(DebugLogs) Debug.Log($"Player spawned {stats.name} (Cost: {stats._spawnCost})");
        
        return spawnedUnit;
    }

    public GameObject SpawnPlayer(GameObject playerPrefab)
    {
        if (!spawningEnabled)
        {
            Debug.Log("Spawning is disabled");
            return null;
        }

        if (PlayerLivesManager.Instance != null && !PlayerLivesManager.Instance.canSpawnMore)
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

        if (Money.Instance.CurrentMoney < cost)
        {
            Debug.Log($"Not enough money to spawn player! Need {cost}, have {Money.Instance.CurrentMoney}");
            return null;
        }

        Money.Instance.CurrentMoney -= cost;

        GameObject spawnedPlayer = Instantiate(
            playerPrefab,
            spawnLocation.position,
            spawnLocation.rotation,
            playerContainer
        );

        // Set player to Character/MidLane
        SetCharacterLayer(spawnedPlayer);

        if (PlayerSwitch.Instance != null)
            PlayerSwitch.Instance.AddPlayer(spawnedPlayer);
        else Debug.Log($"[SpawnObjects] playerSwitch = null");

        if (PlayerLivesManager.Instance != null)
            PlayerLivesManager.Instance.GainLife();
        else Debug.Log($"[SpawnObjects] playerLivesManager = null");

        if (DebugLogs) Debug.Log($"Player spawned (Cost: {cost}) on layer: {LayerMask.LayerToName(spawnedPlayer.layer)}");

        return spawnedPlayer;
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