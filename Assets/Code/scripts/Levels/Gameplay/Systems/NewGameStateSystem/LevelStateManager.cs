using System.Collections.Generic;
using UnityEngine;
using UltEvents;

/// <summary>
/// Main state machine for level progression.
/// Manages transitions between different level states (intro, scouting, combat phases, win/loss).
/// This replaces the old GameState script with a more modular, configurable approach.
/// </summary>
public class LevelStateManager : MonoBehaviour
{
    public static LevelStateManager Instance { get; private set; }
    
    [Header("State Configuration")]
    [SerializeField] private List<LevelState> levelStates = new List<LevelState>();
    [SerializeField] private int initialStateIndex = 0;
    
    [Header("Manager References")]
    [SerializeField] private SceneActivityManager sceneManager;
    
    [Header("Named State References (Optional)")]
    [SerializeField] private LevelState winState;
    [SerializeField] private LevelState lossState;
    
    [Header("Events")]
    [SerializeField] private UltEvent onLevelStart;
    [SerializeField] private UltEvent onStateChanged;
    
    private LevelState currentState;
    private Dictionary<string, LevelState> statesByName = new Dictionary<string, LevelState>();
    private int currentStateIndex = 0;
    
    // Public accessors
    public SceneActivityManager SceneManager => sceneManager;
    public LevelState CurrentState => currentState;
    public int CurrentStateIndex => currentStateIndex;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Find SceneActivityManager if not assigned
        if (sceneManager == null)
        {
            sceneManager = FindAnyObjectByType<SceneActivityManager>();
            if (sceneManager == null)
            {
                Debug.LogError("SceneActivityManager not found! Please assign it in the inspector.");
            }
        }
        
        // Initialize all states
        InitializeStates();
    }
    
    void Start()
    {
        // Notify level start
        onLevelStart?.Invoke();
        
        // Start with the initial state
        if (levelStates.Count > initialStateIndex && initialStateIndex >= 0)
        {
            TransitionToState(initialStateIndex);
        }
        else
        {
            Debug.LogError($"Invalid initial state index: {initialStateIndex}. Total states: {levelStates.Count}");
        }
    }
    
    void Update()
    {
        currentState?.UpdateState();
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void InitializeStates()
    {
        foreach (var state in levelStates)
        {
            if (state != null)
            {
                state.Initialize(this);
                statesByName[state.StateName] = state;
            }
        }
        
        // Also add named references to dictionary
        if (winState != null)
        {
            statesByName["Win"] = winState;
            winState.Initialize(this);
        }
        if (lossState != null)
        {
            statesByName["Loss"] = lossState;
            lossState.Initialize(this);
        }
    }
    
    /// <summary>
    /// Transition to the next state in the list
    /// </summary>
    public void TransitionToNextState()
    {
        int nextIndex = currentStateIndex + 1;
        if (nextIndex < levelStates.Count)
        {
            TransitionToState(nextIndex);
        }
        else
        {
            Debug.LogWarning($"No next state available after state {currentStateIndex}");
        }
    }
    
    /// <summary>
    /// Transition to the previous state in the list
    /// </summary>
    public void TransitionToPreviousState()
    {
        int prevIndex = currentStateIndex - 1;
        if (prevIndex >= 0)
        {
            TransitionToState(prevIndex);
        }
        else
        {
            Debug.LogWarning("No previous state available");
        }
    }
    
    /// <summary>
    /// Transition to a state by index
    /// </summary>
    public void TransitionToState(int stateIndex)
    {
        if (stateIndex < 0 || stateIndex >= levelStates.Count)
        {
            Debug.LogError($"Invalid state index: {stateIndex}. Valid range: 0-{levelStates.Count - 1}");
            return;
        }
        
        // Exit current state
        currentState?.ExitState();
        
        // Update state
        currentStateIndex = stateIndex;
        currentState = levelStates[stateIndex];
        
        // Enter new state
        currentState.EnterState();
        
        // Notify listeners
        onStateChanged?.Invoke();
    }
    
    /// <summary>
    /// Transition to a state by name
    /// </summary>
    public void TransitionToState(string stateName)
    {
        if (statesByName.TryGetValue(stateName, out LevelState state))
        {
            // Try to find index in main list
            int index = levelStates.IndexOf(state);
            if (index >= 0)
            {
                TransitionToState(index);
            }
            else
            {
                // State exists but not in main list (probably win/loss)
                currentState?.ExitState();
                currentState = state;
                currentState.EnterState();
                onStateChanged?.Invoke();
            }
        }
        else
        {
            Debug.LogError($"State '{stateName}' not found!");
        }
    }
    
    /// <summary>
    /// Transition to the win state
    /// </summary>
    public void TransitionToWinState()
    {
        if (winState != null)
        {
            TransitionToState("Win");
        }
        else
        {
            Debug.LogError("Win state not assigned!");
        }
    }
    
    /// <summary>
    /// Transition to the loss state
    /// </summary>
    public void TransitionToLossState()
    {
        if (lossState != null)
        {
            TransitionToState("Loss");
        }
        else
        {
            Debug.LogError("Loss state not assigned!");
        }
    }
    
    /// <summary>
    /// Get a state by name (useful for other scripts to check states)
    /// </summary>
    public LevelState GetState(string stateName)
    {
        statesByName.TryGetValue(stateName, out LevelState state);
        return state;
    }
    
    /// <summary>
    /// Check if we're in a specific state
    /// </summary>
    public bool IsInState(string stateName)
    {
        return currentState != null && currentState.StateName == stateName;
    }
    
    /// <summary>
    /// Check if we're in a specific state by reference
    /// </summary>
    public bool IsInState(LevelState state)
    {
        return currentState == state;
    }
}