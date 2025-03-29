using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CpuController : MonoBehaviour
{
    [SerializeField] private List<ScriptableStats> spawnables = new List<ScriptableStats>();
    [SerializeField] private SpawnObjects spawnObjects;  // Ensure spawnObjects is properly assigned

    // Reference to the InputAction asset
    private InputActionMap actionMap;
    private InputAction spawn1Action;
    private InputAction spawn2Action;
    private InputAction spawn3Action;
    private InputAction spawn4Action;
    private InputAction spawn5Action;
    private InputAction spawn6Action;
    private InputAction spawn7Action;
    private InputAction spawn8Action;
    private InputAction spawn9Action;
    private InputAction spawn0Action;
     private InputSystem_Actions inputActions;

    private void Awake()
    {
        
        inputActions = new InputSystem_Actions();
        inputActions.Enable();  
        actionMap = inputActions.CPUcontroller;  

        // Bind the actions to methods
        spawn1Action = actionMap["Spawn1"];
        spawn2Action = actionMap["Spawn2"];
        spawn3Action = actionMap["Spawn3"];
        spawn4Action = actionMap["Spawn4"];
        spawn5Action = actionMap["Spawn5"];
        spawn6Action = actionMap["Spawn6"];
        spawn7Action = actionMap["Spawn7"];
        spawn8Action = actionMap["Spawn8"];
        spawn9Action = actionMap["Spawn9"];
        spawn0Action = actionMap["Spawn0"];
        
        // Set up action callbacks
        spawn1Action.performed += context => Spawn(context, 0);
        spawn2Action.performed += context => Spawn(context, 1);
        spawn3Action.performed += context => Spawn(context, 2);
        spawn4Action.performed += context => Spawn(context, 3);
        spawn5Action.performed += context => Spawn(context, 4);
        spawn6Action.performed += context => Spawn(context, 5);
        spawn7Action.performed += context => Spawn(context, 6);
        spawn8Action.performed += context => Spawn(context, 7);
        spawn9Action.performed += context => Spawn(context, 8);
        spawn0Action.performed += context => Spawn(context, 9);
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
        Debug.Log("here");
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
