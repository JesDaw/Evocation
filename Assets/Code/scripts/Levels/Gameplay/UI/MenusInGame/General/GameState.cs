using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class GameState : MonoBehaviour
{
    bool GameIsPaused = false;
    

    // For controlling game machanics
    [SerializeField] Money _moneyMachanic;
    [SerializeField] Timer _timeMachanic;
    [SerializeField] PlayerStateMachine _playerStateMachine;
    [SerializeField] SpawnObjects _playerSpawnObjects, _enemySpawnObjects;
    [SerializeField] internal UltEvents.UltEvent LevelPartOne, LevelPartTwo, LevelPartThree;
    //mjusic
    [SerializeField] internal UltEvents.UltEvent TrackfadeInOne, TrackfadeInTwo, TrackfadeInThree;
    [SerializeField] internal UltEvents.UltEvent TrackfadeOutOne, TrackfadeOutTwo, TrackfadeOutThree;
    public LevelState currentlevelState;
    InputAction engaugeAction, toggleCharacterSeceltAction;

    SceneActivityManager sceneMgr;
    public enum LevelState
    {
        Intro,
        Scouting,
        EngaugmentPartOne,
        EngaugmentPartTwo,
        EngaugmentPartThree,
        Win,
        Loose,
        OutTro
    }

    void Awake()
    {
        engaugeAction = InputSystem.actions.FindAction("StartEngaugment");
        toggleCharacterSeceltAction = InputSystem.actions.FindAction("ToggleCharacterSelect");
    }

    void OnEnable()
    {
        if (engaugeAction != null) engaugeAction.Enable();
        if (toggleCharacterSeceltAction != null) toggleCharacterSeceltAction.Enable();
    }
    private void OnDisable()
    {
        if (engaugeAction != null) engaugeAction.Disable();
        if (toggleCharacterSeceltAction != null) toggleCharacterSeceltAction.Disable();
    }

    void Start()
    {
        // Find the SceneActivityManager!
        foreach (var obj in Resources.FindObjectsOfTypeAll<SceneActivityManager>())
        {
            sceneMgr = obj;
        }
        Debug.Assert(sceneMgr != null);

       // if (_moneyMachanic == null) { }
        //if (_timeMachanic == null) { }
        //if (_playerStateMachine == null) { }
        //if (_playerSpawnObjects == null) { }
        //if (_enemySpawnObjects == null) { }
        HandleLevelIntro();
    }

    //=======================Game States=======================================
  internal void HandleLevelIntro()
{
    currentlevelState = LevelState.Intro;
    // optional animation sequence and dialogue
    // Delay the transition to Scouting so Start() finishes first
    HandleLevelScoutingFaze();
}
    public void HandleLevelScoutingFaze()
    {
        currentlevelState = LevelState.Scouting;
        // make player is in free cam and cant switch to player
        if (_playerSpawnObjects != null) _playerSpawnObjects.SpawningIsActive = false;
        else Debug.LogError("_playerSpawnObjects is null");
        if (_enemySpawnObjects != null) _enemySpawnObjects.SpawningIsActive = false;
        else Debug.LogError("_enemySpawnObjects is null");
        if (_timeMachanic != null) _timeMachanic.TimeIsActive = false;
        else Debug.LogError("_timeMachanic is null");
        if (_moneyMachanic != null) _moneyMachanic.MoneyIsActive = false;
        else Debug.LogError("_moneyMachanic is null");

        StartTrackOne();
    }
    public void ToggleScaracterSelectMenu(InputAction.CallbackContext context)
    {
        if (currentlevelState != LevelState.Scouting || !context.performed) return;
        Debug.Log("Character secect button pressed");
        // open character select screen
    }

    public void OnEngaugeButtonPressed(InputAction.CallbackContext context)
    {
        Debug.Log("OnEngaugeButtonPressed");
        if (currentlevelState != LevelState.Scouting || !context.performed) return;
        //check if they are sure
        // 3, 2, 1 go thing
        EngaugmentPartOne();
    }

    public void EngaugmentPartOne()
    {
        currentlevelState = LevelState.EngaugmentPartOne;
        //activate music
        _playerSpawnObjects.SpawningIsActive = true;
        _enemySpawnObjects.SpawningIsActive = true;
        _timeMachanic.TimeIsActive = true;
        _moneyMachanic.MoneyIsActive = true;
        StartTrackTwo();
        LevelPartOne?.Invoke();
    }
    public void EngaugmentPartTwo()
    {
        currentlevelState = LevelState.EngaugmentPartTwo;
        LevelPartTwo?.Invoke();
    }
    public void EngaugmentPartThree()
    {
        currentlevelState = LevelState.EngaugmentPartThree;
        LevelPartThree?.Invoke();
    }

    public void HandleLevelWin()
    {
        currentlevelState = LevelState.Win;
        sceneMgr.Activate("Victory");
        Time.timeScale = 0;
    }

    public void HandleLevelLoss()
    {
        currentlevelState = LevelState.Loose;
        sceneMgr.Activate("Defeat");
        Time.timeScale = 0;
    }

    //=======================Music tracks=======================================
    public void StartTrackOne()
    {
       TrackfadeInOne?.Invoke();
    }
    public void StartTrackTwo()
    {
       TrackfadeInTwo?.Invoke();
    }
    public void StartTrackThree()
    {
        TrackfadeInThree?.Invoke();
    }
}
