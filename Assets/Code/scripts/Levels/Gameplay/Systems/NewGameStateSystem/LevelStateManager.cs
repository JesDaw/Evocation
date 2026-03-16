using System.Collections.Generic;
using UnityEngine;
using UltEvents;

public class LevelStateManager : MonoBehaviour
{
    public static LevelStateManager Instance { get; private set; }
    
    [Header("State Configuration")]
    [SerializeReference, SubclassSelector] 
    private List<LevelState> levelStates = new List<LevelState>();
    
    [SerializeField] private int initialStateIndex = 0;
    
    [Header("Manager References")]
    [SerializeField] private SceneActivityManager sceneManager;
    
    [Header("Named State References (Optional)")]
    [SerializeReference, SubclassSelector] private LevelState winState;
    [SerializeReference, SubclassSelector] private LevelState lossState;
    
    [Header("Events")]
    [SerializeField] private UltEvent onLevelStart;
    [SerializeField] private UltEvent onStateChanged;
    
    private LevelState currentState;
    private Dictionary<string, LevelState> statesByName = new Dictionary<string, LevelState>();
    private int currentStateIndex = 0;
    
    public SceneActivityManager SceneManager => sceneManager;
    public LevelState CurrentState => currentState;
    
    void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        
        if (sceneManager == null) sceneManager = FindAnyObjectByType<SceneActivityManager>();
        InitializeStates();
    }
    
    void Start()
    {
        onLevelStart?.Invoke();
        if (levelStates.Count > initialStateIndex && initialStateIndex >= 0)
            TransitionToState(initialStateIndex);
    }
    
    void Update() => currentState?.UpdateState();

    private void InitializeStates()
    {
        statesByName.Clear();
        foreach (var state in levelStates)
        {
            if (state != null)
            {
                state.Initialize(this);
                statesByName[state.StateName] = state;
            }
        }
        
        if (winState != null) { statesByName["Win"] = winState; winState.Initialize(this); }
        if (lossState != null) { statesByName["Loss"] = lossState; lossState.Initialize(this); }
    }

    public bool IsInState(string name)
    {
        return currentState != null && currentState.stateName == name;
    }
    
    public void TransitionToState(int stateIndex)
    {
        if (stateIndex < 0 || stateIndex >= levelStates.Count) return;
        currentState?.ExitState();
        currentStateIndex = stateIndex;
        currentState = levelStates[stateIndex];
        currentState.EnterState();
        onStateChanged?.Invoke();
    }

    public void TransitionToState(string stateName)
    {
        if (statesByName.TryGetValue(stateName, out LevelState state))
        {
            int index = levelStates.IndexOf(state);
            if (index >= 0) TransitionToState(index);
            else
            {
                currentState?.ExitState();
                currentState = state;
                currentState.EnterState();
                onStateChanged?.Invoke();
            }
        }
    }

    public void TransitionToNextState() => TransitionToState(currentStateIndex + 1);
    public void TransitionToWinState() => TransitionToState("Win");
    public void TransitionToLossState() => TransitionToState("Loss");
}