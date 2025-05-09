using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnerController : MonoBehaviour
{
    [SerializeField] private List<ScriptableStats> spawnables = new List<ScriptableStats>();
    [SerializeField] private SpawnObjects spawnObjects;
    [SerializeField] GameObject _Player;

    private InputSystem_Actions inputActions; 
    private InputActionMap actionMap;

    public InputAction spawn1Action;
    private InputAction spawn2Action;
    private InputAction spawn3Action;
    private InputAction spawn4Action;
    private InputAction spawn5Action;
    private InputAction spawn6Action;
    private InputAction spawn7Action;
    private InputAction spawn8Action;
    private InputAction spawn9Action;
    private InputAction spawnPlayer;

    private void Awake()
    {
        // Create and store the InputSystem_Actions instance
        inputActions = new InputSystem_Actions();
        actionMap = inputActions.SpawnerController;

        // Assign input actions
        spawn1Action = actionMap["Spawn1"];
        spawn2Action = actionMap["Spawn2"];
        spawn3Action = actionMap["Spawn3"];
        spawn4Action = actionMap["Spawn4"];
        spawn5Action = actionMap["Spawn5"];
        spawn6Action = actionMap["Spawn6"];
        spawn7Action = actionMap["Spawn7"];
        spawn8Action = actionMap["Spawn8"];
        spawn9Action = actionMap["Spawn9"];
        spawnPlayer = actionMap["SpawnPlayer"];

        // Bind input actions to functions
        spawn1Action.performed += Spawn1;
        spawn2Action.performed += Spawn2;
        spawn3Action.performed += Spawn3;
        spawn4Action.performed += Spawn4;
        spawn5Action.performed += Spawn5;
        spawn6Action.performed += Spawn6;
        spawn7Action.performed += Spawn7;
        spawn8Action.performed += Spawn8;
        spawn9Action.performed += Spawn9;
        spawnPlayer.performed += SpawnPlayerPerformed; 
    }

    private void SpawnPlayerPerformed(InputAction.CallbackContext context)
    {
        if (_Player != null && spawnObjects != null)
        {
            spawnObjects.SpawnPlayer(_Player);
        }
        else
        {
            Debug.LogWarning("Player GameObject or SpawnObjects reference not set in SpawnerController.");
        }
    }

    public void Spawn1(InputAction.CallbackContext context) { StartCoroutine(Spawn(context, 0)); }
    public void Spawn2(InputAction.CallbackContext context) { StartCoroutine(Spawn(context, 1)); }
    public void Spawn3(InputAction.CallbackContext context) { StartCoroutine(Spawn(context, 2)); }
    public void Spawn4(InputAction.CallbackContext context) { StartCoroutine(Spawn(context, 3)); }
    public void Spawn5(InputAction.CallbackContext context) { StartCoroutine(Spawn(context, 4)); }
    public void Spawn6(InputAction.CallbackContext context) { StartCoroutine(Spawn(context, 5)); }
    public void Spawn7(InputAction.CallbackContext context) { StartCoroutine(Spawn(context, 6)); }
    public void Spawn8(InputAction.CallbackContext context) { StartCoroutine(Spawn(context, 7)); }
    public void Spawn9(InputAction.CallbackContext context) { StartCoroutine(Spawn(context, 8)); }

    public float CoolDown;
    private bool AlreadySpawned = false;

    private void OnEnable()
    {
        if (actionMap != null)
        {
            actionMap.Enable();
        }
    }

    private void OnDisable()
    {
        if (actionMap != null)
        {
            actionMap.Disable();
        }
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            spawn1Action.performed -= Spawn1;
            spawn2Action.performed -= Spawn2;
            spawn3Action.performed -= Spawn3;
            spawn4Action.performed -= Spawn4;
            spawn5Action.performed -= Spawn5;
            spawn6Action.performed -= Spawn6;
            spawn7Action.performed -= Spawn7;
            spawn8Action.performed -= Spawn8;
            spawn9Action.performed -= Spawn9;
            spawnPlayer.performed -= SpawnPlayerPerformed;

            inputActions.Dispose();
            inputActions = null; 
        }
    }

    IEnumerator Spawn(InputAction.CallbackContext context, int index)
    {
        if (AlreadySpawned)
        {
            Debug.Log("Cooldown active. Waiting...");
            yield break; // Immediately exit if still on cooldown
        }

        if (spawnables != null && index >= 0 && index < spawnables.Count)
        {
            if (spawnObjects != null && spawnables[index] != null)
            {
                spawnObjects.Spawn(spawnables[index]);
                Debug.Log("Spawned object at index: " + index);

                AlreadySpawned = true;

                yield return new WaitForSeconds(CoolDown);

                AlreadySpawned = false;
            }
            else
            {
                Debug.LogWarning("SpawnObjects reference or spawnable at index " + index + " is null.");
            }
        }
        else
        {
            Debug.LogWarning("Invalid index or spawnables list not set: No spawnable object at index " + index);
        }
    }
}