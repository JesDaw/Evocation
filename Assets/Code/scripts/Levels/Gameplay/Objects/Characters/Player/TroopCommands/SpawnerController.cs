using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Player spawn controller - connects input to spawning
/// </summary>
public class SpawnController : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] List<ScriptableStats> spawnableCPUs = new List<ScriptableStats>();

    [Header("Player Spawning")]
    [SerializeField] GameObject playerPrefab;

    [Header("Debug")]
    [SerializeField] bool showDebugLogs = false;
    public static SpawnController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (GlobalInputManager.Instance == null)
        {
            Debug.LogError("GlobalInputManager not found! Spawning will not work.");
            return;
        }

        SubscribeToInput();
    }

    void OnDestroy()
    {
        UnsubscribeFromInput();
    }

    void SubscribeToInput()
    {
        var input = GlobalInputManager.Instance.InputActions.SpawnerController;

        input.Spawn1.performed += ctx => TrySpawnCPU(0);
        input.Spawn2.performed += ctx => TrySpawnCPU(1);
        input.Spawn3.performed += ctx => TrySpawnCPU(2);
        input.Spawn4.performed += ctx => TrySpawnCPU(3);
        input.Spawn5.performed += ctx => TrySpawnCPU(4);
        input.Spawn6.performed += ctx => TrySpawnCPU(5);
        input.Spawn7.performed += ctx => TrySpawnCPU(6);
        input.Spawn8.performed += ctx => TrySpawnCPU(7);
        input.Spawn9.performed += ctx => TrySpawnCPU(8);
        input.SpawnPlayer.performed += ctx => SpawnPlayer();

        if (showDebugLogs)
            Debug.Log("SpawnController: Input subscribed");
    }

    void UnsubscribeFromInput()
    {
        if (GlobalInputManager.Instance == null) return;

        var input = GlobalInputManager.Instance.InputActions.SpawnerController;

        input.Spawn1.performed -= ctx => TrySpawnCPU(0);
        input.Spawn2.performed -= ctx => TrySpawnCPU(1);
        input.Spawn3.performed -= ctx => TrySpawnCPU(2);
        input.Spawn4.performed -= ctx => TrySpawnCPU(3);
        input.Spawn5.performed -= ctx => TrySpawnCPU(4);
        input.Spawn6.performed -= ctx => TrySpawnCPU(5);
        input.Spawn7.performed -= ctx => TrySpawnCPU(6);
        input.Spawn8.performed -= ctx => TrySpawnCPU(7);
        input.Spawn9.performed -= ctx => TrySpawnCPU(8);
        input.SpawnPlayer.performed -= ctx => SpawnPlayer();

        if (showDebugLogs)
            Debug.Log("SpawnController: Input unsubscribed");
    }

    void TrySpawnCPU(int index)
    {
        // Validation
        if (SpawnObjects.PlayerInstance == null)
        {
            if (showDebugLogs)Debug.LogError("No spawner assigned on player spawner!");
            return;
        }

        if (index < 0 || index >= spawnableCPUs.Count)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Invalid spawn index: {index}");
            return;
        }

        ScriptableStats stats = spawnableCPUs[index];
        if (stats == null)
        {
            if (showDebugLogs)Debug.LogWarning($"No stats at index {index}");
            return;
        }

        // Spawn through the spawner (handles money deduction)
        GameObject spawned = SpawnObjects.PlayerInstance.SpawnFromPlayer(stats);

        if (spawned != null)
        {
            FModAudioManager.instance.PlaySoundByName("spawnTroop");
            //FModAudioManager.instance.PlaySoundByName(stats.SpawnSoundName);
            if (showDebugLogs)Debug.Log($"Player spawned: {stats.name}");
        }
    }

    void SpawnPlayer()
    {
        if (SpawnObjects.PlayerInstance == null)
        {
            if (showDebugLogs)Debug.LogError("No spawner assigned!");
            return;
        }

        if (playerPrefab == null)
        {
            if (showDebugLogs)Debug.LogWarning("No player prefab assigned!");
            return;
        }

        GameObject spawned = SpawnObjects.PlayerInstance.SpawnPlayer(playerPrefab);

        if (spawned != null)
        {
            FModAudioManager.instance.PlaySoundByName("spawnTroop");
            if (showDebugLogs)Debug.Log("Player character spawned");
        }
    }

    /// <summary>
    /// Add a CPU to the spawn list (for unlocking units)
    /// </summary>
    public void EquipCPU(ScriptableStats stats)
    {
        if (stats == null) return;
        if (!spawnableCPUs.Contains(stats))
        {
            spawnableCPUs.Add(stats);
            if (showDebugLogs)Debug.Log($"Equipped: {stats.name}");
        }
    }

    /// <summary>
    /// Remove a CPU from the spawn list
    /// </summary>
    public void UnequipCPU(ScriptableStats stats)
    {
        if (stats == null) return;
        if (spawnableCPUs.Contains(stats))
        {
            spawnableCPUs.Remove(stats);
            if (showDebugLogs)Debug.Log($"Unequipped: {stats.name}");
        }
    }
}