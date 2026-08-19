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

    const string ALLIES_MID_LAYER = "Allies/MidLane";
    const string ENEMY_MID_LAYER = "Enemy/MidLane";
    const string PLAYER_MID_LAYER = "Player/MidLane";

    public bool SpawningIsActive
    {
        get { return spawningEnabled; }
        set { spawningEnabled = value; }
    }

    #region Setup
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

    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;
    }
    #endregion

    public GameObject SpawnFromAISpawner(ScriptableStats stats, bool SpawnForFree = false)
    {
        if (stats == null)
        {
            Debug.LogWarning($"stats does not exist on AI spawner");
            return null;
        }
        return SpawnCPU(stats);
    }
    #region player
    public GameObject SpawnFromPlayer(ScriptableStats stats, bool SpawnForFree = false)
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

        if(!SpawnForFree) 
        {
            Money.Instance.spendMoney(stats._spawnCost);
        }

        GameObject spawnedUnit = SpawnCPU(stats);
        
        if(DebugLogs) Debug.Log($"Player spawned {stats.name} (Cost: {stats._spawnCost})");
        
        return spawnedUnit;
    }
    
    public GameObject SpawnPlayer(GameObject playerPrefab, bool SpawnForFree = false)
    {
        if (!spawningEnabled)
        {
            Debug.Log("Spawning is disabled");
            return null;
        }

        if (PlayerLivesManager.Instance != null && !PlayerLivesManager.Instance.canSpawnMore)
        {
            //Debug.Log("Cannot spawn more players!");
            return null;
        }

        ScriptableStats playerStats = playerPrefab.GetComponent<Stats>().scriptableStats;
        if (playerStats == null)
        {
            Debug.LogError("Player prefab has no Stats component!");
            return null;
        }

        if (Money.Instance.CurrentMoney < playerStats._spawnCost)
        {
            Debug.Log($"Not enough money to spawn player! Need {playerStats._spawnCost}, have {Money.Instance.CurrentMoney}");
            return null;
        }

        if(!SpawnForFree) Money.Instance.spendMoney(playerStats._spawnCost);

        GameObject spawnedPlayer = Instantiate(
            playerPrefab,
            spawnLocation.position,
            spawnLocation.rotation,
            playerContainer
        );

        // Set player to Character/MidLane
        SetCharacterLayer(spawnedPlayer, true);

        if (PlayerSwitch.Instance != null)
            PlayerSwitch.Instance.AddPlayer(spawnedPlayer);
        else Debug.Log($"[SpawnObjects] playerSwitch = null");

        if (PlayerLivesManager.Instance != null)
            PlayerLivesManager.Instance.GainLife();
        else Debug.Log($"[SpawnObjects] playerLivesManager = null");

        if (DebugLogs) Debug.Log($"Player spawned (Cost: {playerStats._spawnCost}) on layer: {LayerMask.LayerToName(spawnedPlayer.layer)}");

        return spawnedPlayer;
    }
    #endregion
// Spawn a CPU unit (called by AI and Player)
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


    #region CPUconfig
    private void SetCharacterLayer(GameObject unit, bool isPlayer = false)
    {
        string layerName = "";
        if(enemySpawner) layerName = ENEMY_MID_LAYER;
        else if(!enemySpawner) layerName = ALLIES_MID_LAYER;
        if(isPlayer) layerName = PLAYER_MID_LAYER;
        
        int layer = LayerMask.NameToLayer(layerName);
        
        if (layer == -1)
        {
            Debug.LogError($"Layer '{layerName}' not found! Make sure it exists in your project settings.");
            return;
        }

        unit.layer = layer;

        if (DebugLogs) Debug.Log($"Set {unit.name} to layer {layerName} (#{layer})");
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
     #endregion
    

    
}