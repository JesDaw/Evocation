using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class SpawnController : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] List<ScriptableStats> spawnableCPUs = new List<ScriptableStats>();
    [SerializeField] SpawnObjects spawnObjects;

    [Header("Player Spawning")]
    [SerializeField] GameObject playerPrefab;

    void Start()
    {
        if (GlobalInputManager.Instance == null)
        {
            Debug.LogError("GlobalInputManager not found! Spawning will not work.");
            return;
        }

        var input = GlobalInputManager.Instance.InputActions.SpawnerController;

        input.Spawn1.performed += Spawn1Performed;
        input.Spawn2.performed += Spawn2Performed;
        input.Spawn3.performed += Spawn3Performed;
        input.Spawn4.performed += Spawn4Performed;
        input.Spawn5.performed += Spawn5Performed;
        input.Spawn6.performed += Spawn6Performed;
        input.Spawn7.performed += Spawn7Performed;
        input.Spawn8.performed += Spawn8Performed;
        input.Spawn9.performed += Spawn9Performed;
        input.SpawnPlayer.performed += SpawnPlayerPerformed;

        //Debug.Log("SpawnController: Subscribed to all spawn inputs");
        
        // Check if the action map is enabled
        //Debug.Log($"SpawnerController action map enabled: {input.enabled}");
    }

    void OnDisable()
    {
        if (GlobalInputManager.Instance == null) return;

        var input = GlobalInputManager.Instance.InputActions.SpawnerController;

        input.Spawn1.performed -= Spawn1Performed;
        input.Spawn2.performed -= Spawn2Performed;
        input.Spawn3.performed -= Spawn3Performed;
        input.Spawn4.performed -= Spawn4Performed;
        input.Spawn5.performed -= Spawn5Performed;
        input.Spawn6.performed -= Spawn6Performed;
        input.Spawn7.performed -= Spawn7Performed;
        input.Spawn8.performed -= Spawn8Performed;
        input.Spawn9.performed -= Spawn9Performed;
        input.SpawnPlayer.performed -= SpawnPlayerPerformed;
        
        //Debug.Log("SpawnController: Unsubscribed from all spawn inputs");
    }

    public void EquipCPU(ScriptableStats stats)
    {
        spawnableCPUs.Add(stats);
    }

    public void UnequipCPU(ScriptableStats stats)
    {
        spawnableCPUs.Remove(stats);
    }

    public void Spawn1Performed(InputAction.CallbackContext context)
    {
        //Debug.Log("Spawn1 button pressed!");
        TrySpawnCPU(0, context);
    }
    
    public void Spawn2Performed(InputAction.CallbackContext context)
    {
        //Debug.Log("Spawn2 button pressed!");
        TrySpawnCPU(1, context);
    }
    
    public void Spawn3Performed(InputAction.CallbackContext context)
    {
        //Debug.Log("Spawn3 button pressed!");
        TrySpawnCPU(2, context);
    }
    
    public void Spawn4Performed(InputAction.CallbackContext context)
    {
        //Debug.Log("Spawn4 button pressed!");
        TrySpawnCPU(3, context);
    }
    
    public void Spawn5Performed(InputAction.CallbackContext context)
    {
        //Debug.Log("Spawn5 button pressed!");
        TrySpawnCPU(4, context);
    }
    
    public void Spawn6Performed(InputAction.CallbackContext context)
    {
        //Debug.Log("Spawn6 button pressed!");
        TrySpawnCPU(5, context);
    }
    
    public void Spawn7Performed(InputAction.CallbackContext context)
    {
        //Debug.Log("Spawn7 button pressed!");
        TrySpawnCPU(6, context);
    }
    
    public void Spawn8Performed(InputAction.CallbackContext context)
    {
        //Debug.Log("Spawn8 button pressed!");
        TrySpawnCPU(7, context);
    }
    
    public void Spawn9Performed(InputAction.CallbackContext context)
    {
        //Debug.Log("Spawn9 button pressed!");
        TrySpawnCPU(8, context);
    }

    public void SpawnPlayerPerformed(InputAction.CallbackContext context)
    {
        //Debug.Log("SpawnPlayer button pressed!");
        
        if (playerPrefab != null && spawnObjects != null)
        {
            spawnObjects.SpawnPlayer(playerPrefab);
            //Debug.Log("Player spawned successfully");
        }
        else
        {
            Debug.LogWarning("Player GameObject or SpawnObjects reference not set in SpawnerController.");
        }
    }

    void TrySpawnCPU(int index, InputAction.CallbackContext context)
    {
        //Debug.Log($"TrySpawnCPU called for index {index}, performed: {context.performed}");
        
        if (!context.performed)
        {
            Debug.LogWarning($"Context not performed for spawn {index}");
            return;
        }
        
        if (spawnableCPUs == null)
        {
            Debug.LogError("spawnableCPUs array is null!");
            return;
        }
        
        if (index < 0 || index >= spawnableCPUs.Count)
        {
            Debug.LogWarning($"Invalid spawn index: {index} (list Count: {spawnableCPUs.Count})");
            return;
        }
        
        if (spawnObjects == null)
        {
            Debug.LogError("SpawnObjects reference is null!");
            return;
        }
        
        if (spawnableCPUs[index] == null)
        {
            Debug.LogWarning($"Spawnable at index {index} is null!");
            return;
        }
        
        spawnObjects.Spawn(spawnableCPUs[index]);
        //Debug.Log($"Successfully spawned CPU at index: {index}");
    }
    
    // Add this method to check status at runtime
    void Update()
    {
        // Press L to log the current state
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            LogSpawnControllerState();
        }
    }
    
    void LogSpawnControllerState()
    {
        if (GlobalInputManager.Instance == null)
        {
            Debug.LogError("GlobalInputManager is NULL!");
            return;
        }
        
        var spawnerMap = GlobalInputManager.Instance.InputActions.SpawnerController;
        Debug.Log($"=== SpawnController Status ===");
        Debug.Log($"SpawnerController action map enabled: {spawnerMap.enabled}");
        Debug.Log($"Spawn1 action enabled: {spawnerMap.Spawn1.enabled}");
        Debug.Log($"SpawnObjects reference: {(spawnObjects != null ? "Valid" : "NULL")}");
        Debug.Log($"spawnableCPUs count: {(spawnableCPUs != null ? spawnableCPUs.Count.ToString() : "NULL")}");
    }
}