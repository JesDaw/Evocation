using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class GameStateCaller : MonoBehaviour
{
    [Header("State Atrobutes")]
    [SerializeField] public bool EditTimeScale;
    [SerializeField] [Range(0, 3)] public float TimeScale = 1;
    [SerializeField] public bool EditTimer;
    [SerializeField] public bool StopTimer;
    [SerializeField] public bool EditMoneyGen;
    [SerializeField] public bool StopMoneyGen;
    [SerializeField] public bool EditMoneySpending;
    [SerializeField] public bool StopMoneySpending;
    [SerializeField] public bool EditCharacterBehavior;
    [SerializeField] public bool MakeAllCharactersIdle;
    [SerializeField] public bool KillAllFriends;
    [SerializeField] public bool KillAllFoes;
    [SerializeField] public List<GameObject> ObjectsToEnable = new List<GameObject>();
    [SerializeField] public List<GameObject> ObjectsToDisable = new List<GameObject>();
    [SerializeField] public PlayableDirector CutscenToActivate;

    public UnityEvent StateCalled;

    [Header("State Debugging Stuff")]
    [SerializeField] GameStateManager GameStateManager;
    [SerializeField] public bool StateIsActive;



    string _stateName;
    public string StateName { get { return _stateName; } }

    void Start()
    {
        _stateName = gameObject.name;
        if (GameStateManager == null) Debug.LogError($"gameStateManager isn't attached to state - {_stateName}");
        if (TimeScale < 0)
        {
            Debug.LogWarning($" time scale for state - {_stateName} is < 0");
            TimeScale = 0;
        }
    }


    public void ActivateState()
    {
        if (GameStateManager == null)
        {
            Debug.LogError($"gameStateManager isn't attached to state - {_stateName} so cant activate it");
            return;
        }
        GameStateManager.GameStateChanger(this);
        StateCalled?.Invoke();
    }
}
