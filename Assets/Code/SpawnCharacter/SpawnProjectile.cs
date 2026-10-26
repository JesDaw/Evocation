using UnityEngine;
using UnityEngine.Events;

public class SpawnProjectile : SpecialProjectile
{
    // Direct calls to SpawnCPU do not charge money.
    [SerializeField] bool enemySpawner;
    [SerializeField] ScriptableStats spawnStats;
    [SerializeField] GameObject cpuFriendPrefab;
    [SerializeField] GameObject cpuEnemyPrefab;
    public UnityEvent<GameObject> onSpawn;
    [SerializeField] bool DebugLogs;

    //this script holds some code for enemyspawning but it doesn't uses it,
    //if you want to clean it up feel free to do so :D

    public GameObject SpawnCPU(ScriptableStats stats)
    {
        enemySpawner = attacker._Enemy;

        if (stats == null || cpuFriendPrefab == null || attacker == null)
        {
            Debug.LogWarning(
                "[SpawnObjects] Cannot spawn CPU: missing stats, prefab, or attacker.");

            return null;
        }

        if (cpuFriendPrefab.GetComponent<Stats>() == null)
        {
            Debug.LogError(
                "[SpawnObjects] CPU prefab has no Stats component.");

            return null;
        }

        GameObject cpuPrefab = enemySpawner ? cpuEnemyPrefab : cpuFriendPrefab;
        Transform locationTransform = enemySpawner ? EnemyReferenceHolder.Instance : FriendReferenceHolder.Instance;

        GameObject spawnedUnit = Instantiate(
            cpuPrefab,
            transform.position,
            locationTransform.rotation,
            locationTransform
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

    void SetCharacterLayer(GameObject unit, bool isPlayer = false)
    {
        if (unit == null)
            return;

        string layerName;

        if (isPlayer)
            layerName = SpawnObjects.PLAYER_MID_LAYER;
        else if (enemySpawner)
            layerName = SpawnObjects.ENEMY_MID_LAYER;
        else
            layerName = SpawnObjects.ALLIES_MID_LAYER;

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

    void Start()
    {
        SpawnCPU(spawnStats);
        Destroy(this.gameObject);
    }
}
