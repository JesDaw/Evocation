using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SpawnerController : MonoBehaviour
{
    [SerializeField] private List<ScriptableStats> spawnables = new List<ScriptableStats>();
    [SerializeField] private SpawnObjects spawnObjects; 
    [SerializeField] GameObject _Player;
    [SerializeField] PlayerLivesManager PlayerCountManager;


    // Reference to the InputAction asset
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
    var inputActions = new InputSystem_Actions();
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
    spawnPlayer.performed += SpawnPlayer;
}

    public void Spawn1(InputAction.CallbackContext context){ Spawn(context, 0);}
    public void Spawn2(InputAction.CallbackContext context){ Spawn(context, 1);}
    public void Spawn3(InputAction.CallbackContext context){ Spawn(context, 2);}
    public void Spawn4(InputAction.CallbackContext context){ Spawn(context, 3);}
    public void Spawn5(InputAction.CallbackContext context){ Spawn(context, 4);}
    public void Spawn6(InputAction.CallbackContext context){ Spawn(context, 5);}
    public void Spawn7(InputAction.CallbackContext context){ Spawn(context, 6);}
    public void Spawn8(InputAction.CallbackContext context){ Spawn(context, 7);}
    public void Spawn9(InputAction.CallbackContext context){ Spawn(context, 8);}
    public void SpawnPlayer(InputAction.CallbackContext context)
    { 
        if (PlayerCountManager.GainLife()) spawnObjects.SpawnPlayer(_Player);
        else Debug.Log("here");
    }

    private void OnEnable()
    {
        // Enable the action map to start listening for inputs
        actionMap.Enable();
    }

    private void OnDisable()
    {
        // Disable the action map when not needed
        actionMap.Disable();
    }

    void Spawn(InputAction.CallbackContext context, int index)
    {
        // Check if the index is within bounds of the spawnables list
        if (index >= 0 && index < spawnables.Count) 
        {
            // Valid index: Proceed to spawn object
            spawnObjects.Spawn(spawnables[index]);
        }
        else
        {
            // If index is invalid, log a warning
            Debug.LogWarning("Invalid index: No spawnable object at index " + index);
        }
        // Debug message to check if the spawn action was called
        Debug.Log("Spawn called with index: " + index);
    }
}
