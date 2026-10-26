
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Universal spawner for CPUs and Players.
/// Works with both AI and player spawning controllers.
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

    [Header("Events")]
    [SerializeField] UnityEvent<GameObject> onSpawn;
    [SerializeField] bool DebugLogs = false;

    public static SpawnObjects EnemyInstance { get; private set; }
    public static SpawnObjects PlayerInstance { get; private set; }

    public const string ALLIES_MID_LAYER = "Allies/MidLane";
    public const string ENEMY_MID_LAYER = "Enemy/MidLane";
    public const string PLAYER_MID_LAYER = "Player/MidLane";

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
        if (spawnLocation == null)
            Debug.LogError("[SpawnObjects] No spawn location set!");

        if (cpuPrefab == null)
            Debug.LogError("[SpawnObjects] No CPU prefab assigned!");

        if (Money.Instance == null)
            Debug.LogWarning("[SpawnObjects] No player Money instance found.");
    }

    void OnDestroy()
    {
        if (EnemyInstance == this)
            EnemyInstance = null;

        if (PlayerInstance == this)
            PlayerInstance = null;
    }

    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;
    }

    #endregion

    #region CPU Spawning

    public GameObject Spawn(
        ScriptableStats stats,
        bool SpawnForFree = false)
    {
        if (!spawningEnabled)
        {
            if (DebugLogs)
                Debug.Log("[SpawnObjects] Spawning is disabled.");

            return null;
        }

        if (stats == null)
        {
            Debug.LogWarning("[SpawnObjects] Cannot spawn: stats are null.");
            return null;
        }

        if (cpuPrefab == null || spawnLocation == null)
        {
            Debug.LogError(
                "[SpawnObjects] CPU prefab or spawn location is missing.");

            return null;
        }

        if (cpuPrefab.GetComponent<Stats>() == null)
        {
            Debug.LogError(
                "[SpawnObjects] CPU prefab has no Stats component.");

            return null;
        }

        Money money = null;

        if (!SpawnForFree)
        {
            money = this == EnemyInstance
                ? Money.AIInstance
                : Money.Instance;

            if (money == null)
            {
                Debug.LogError(
                    "[SpawnObjects] Required Money instance is missing.");

                return null;
            }

            if (money.CurrentMoney < stats._spawnCost)
            {
                if (DebugLogs)
                {
                    Debug.Log(
                        $"Not enough money. Need {stats._spawnCost}, " +
                        $"have {money.CurrentMoney}.");
                }

                return null;
            }
        }

        // Spawn first; deduct money only after success.
        GameObject spawnedUnit = SpawnCPU(stats);

        if (spawnedUnit == null)
            return null;

        if (!SpawnForFree)
            money.spendMoney(stats._spawnCost);

        if (DebugLogs)
        {
            Debug.Log(
                $"Spawned {stats.name}. " +
                $"Cost: {(SpawnForFree ? 0 : stats._spawnCost)}");
        }

        return spawnedUnit;
    }

    // Direct calls to SpawnCPU do not charge money.
    public GameObject SpawnCPU(ScriptableStats stats)
    {
        if (!spawningEnabled)
            return null;

        if (stats == null || cpuPrefab == null ||
            spawnLocation == null)
        {
            Debug.LogWarning(
                "[SpawnObjects] Cannot spawn CPU: missing stats, prefab, or spawn location.");

            return null;
        }

        if (cpuPrefab.GetComponent<Stats>() == null)
        {
            Debug.LogError(
                "[SpawnObjects] CPU prefab has no Stats component.");

            return null;
        }

        GameObject spawnedUnit = Instantiate(
            cpuPrefab,
            spawnLocation.position,
            spawnLocation.rotation,
            cpuContainer
        );

        Stats unitStats = spawnedUnit.GetComponent<Stats>();

        if (unitStats == null)
        {
            Debug.LogError(
                "[SpawnObjects] Spawned unit has no Stats component.");

            Destroy(spawnedUnit);
            return null;
        }

        unitStats.scriptableStats = stats;
        unitStats._Enemy = enemySpawner;
        unitStats.InitializeStats();

        string unitTag = enemySpawner ? "Enemy" : "Allies";
        spawnedUnit.tag = unitTag;

        SetCharacterLayer(spawnedUnit);
        RandomizeAppearancePosition(spawnedUnit);

        onSpawn?.Invoke(spawnedUnit);

        if (DebugLogs)
        {
            Debug.Log(
                $"Spawned {unitTag} on layer: " +
                $"{LayerMask.LayerToName(spawnedUnit.layer)}");
        }

        return spawnedUnit;
    }

    #endregion

    #region Player Spawning

    public GameObject SpawnPlayer(
        GameObject playerPrefab,
        bool SpawnForFree = false)
    {
        if (!spawningEnabled)
            return null;

        if (playerPrefab == null)
        {
            Debug.LogWarning("[SpawnObjects] Player prefab is null.");
            return null;
        }

        if (spawnLocation == null)
        {
            Debug.LogError("[SpawnObjects] No spawn location assigned.");
            return null;
        }

        if (PlayerLivesManager.Instance != null &&
            !PlayerLivesManager.Instance.canSpawnMore)
            return null;

        Stats prefabStats = playerPrefab.GetComponent<Stats>();

        if (prefabStats == null)
        {
            Debug.LogError(
                "[SpawnObjects] Player prefab has no Stats component.");

            return null;
        }

        ScriptableStats playerStats = prefabStats.scriptableStats;

        if (playerStats == null)
        {
            Debug.LogError(
                "[SpawnObjects] Player prefab has no ScriptableStats assigned.");

            return null;
        }

        Money money = Money.Instance;

        if (!SpawnForFree)
        {
            if (money == null)
            {
                Debug.LogError(
                    "[SpawnObjects] Player Money instance is missing.");

                return null;
            }

            if (money.CurrentMoney < playerStats._spawnCost)
            {
                if (DebugLogs)
                {
                    Debug.Log(
                        $"Not enough money to spawn player. " +
                        $"Need {playerStats._spawnCost}, " +
                        $"have {money.CurrentMoney}.");
                }

                return null;
            }
        }

        GameObject spawnedPlayer = Instantiate(
            playerPrefab,
            spawnLocation.position,
            spawnLocation.rotation,
            playerContainer
        );

        // Deduct only after successful instantiation.
        if (!SpawnForFree)
            money.spendMoney(playerStats._spawnCost);

        SetCharacterLayer(spawnedPlayer, true);

        if (PlayerSwitch.Instance != null)
            PlayerSwitch.Instance.AddPlayer(spawnedPlayer);
        else if (DebugLogs)
            Debug.Log("[SpawnObjects] PlayerSwitch instance is null.");

        if (PlayerLivesManager.Instance != null)
            PlayerLivesManager.Instance.GainLife();
        else if (DebugLogs)
            Debug.Log("[SpawnObjects] PlayerLivesManager instance is null.");

        if (DebugLogs)
        {
            Debug.Log(
                $"Player spawned. Cost: " +
                $"{(SpawnForFree ? 0 : playerStats._spawnCost)}");
        }

        return spawnedPlayer;
    }

    #endregion

    #region CPU Configuration

    void SetCharacterLayer(GameObject unit, bool isPlayer = false)
    {
        if (unit == null)
            return;

        string layerName;

        if (isPlayer)
            layerName = PLAYER_MID_LAYER;
        else if (enemySpawner)
            layerName = ENEMY_MID_LAYER;
        else
            layerName = ALLIES_MID_LAYER;

        int layer = LayerMask.NameToLayer(layerName);

        if (layer == -1)
        {
            Debug.LogError(
                $"Layer '{layerName}' not found in project settings.");

            return;
        }

        unit.layer = layer;

        if (DebugLogs)
        {
            Debug.Log(
                $"Set {unit.name} to layer {layerName} (#{layer}).");
        }
    }

    void RandomizeAppearancePosition(GameObject unit)
    {
        if (unit == null)
            return;

        Transform appearance = unit.transform.Find("CpuAppearance");

        if (appearance == null)
            appearance = unit.transform.Find("Appearance");

        if (appearance == null)
            return;

        float randomY = Random.Range(-0.5f, 0.5f);
        Vector3 pos = appearance.position;

        pos.y += randomY;
        pos.z += randomY;

        appearance.position = pos;
    }

    #endregion
}
