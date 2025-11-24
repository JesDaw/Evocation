using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SpawnController : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] ScriptableStats[] spawnableCPUs;
    [SerializeField] SpawnObjects spawnObjects;


    [Header("Player Spawning")]
    [SerializeField] GameObject playerPrefab;

    void Start()
    {
        if (GlobalInputManager.Instance == null)
        {
            Debug.LogWarning("GlobalInputManager not found.");
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

        // Make sure the SpawnerController map is active
        GlobalInputManager.Instance.EnableCharacterSpawnControls();
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
    }

    public void Spawn1Performed(InputAction.CallbackContext context) => TrySpawnCPU(0, context); 
    public void Spawn2Performed(InputAction.CallbackContext context) => TrySpawnCPU(1, context);
    public void Spawn3Performed(InputAction.CallbackContext context) => TrySpawnCPU(2, context);
    public void Spawn4Performed(InputAction.CallbackContext context) => TrySpawnCPU(3, context);
    public void Spawn5Performed(InputAction.CallbackContext context) => TrySpawnCPU(4, context);
    public void Spawn6Performed(InputAction.CallbackContext context) => TrySpawnCPU(5, context);
    public void Spawn7Performed(InputAction.CallbackContext context) => TrySpawnCPU(6, context);
    public void Spawn8Performed(InputAction.CallbackContext context) => TrySpawnCPU(7, context);
    public void Spawn9Performed(InputAction.CallbackContext context) => TrySpawnCPU(8, context);

    public void SpawnPlayerPerformed(InputAction.CallbackContext context)
    {
        if (playerPrefab != null && spawnObjects != null)
        {
            spawnObjects.SpawnPlayer(playerPrefab);
        }
        else
        {
            Debug.LogWarning("Player GameObject or SpawnObjects reference not set in SpawnerController.");
        }
    }

    void TrySpawnCPU(int index, InputAction.CallbackContext context)
    {
        if (context.performed && spawnableCPUs != null && index >= 0 && index < spawnableCPUs.Length)
        {
            if (spawnObjects != null && spawnableCPUs[index] != null)
            {
                spawnObjects.Spawn(spawnableCPUs[index]);
                //Debug.Log("Spawned object at index: " + index);
            }
            else
            {
                Debug.LogWarning("SpawnObjects reference or spawnable at index " + index + " is null.");
            }
        }
        else
        {
            Debug.LogWarning("Invalid index or spawnables list not set.");
        }
    }
}
