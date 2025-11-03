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
    [SerializeField] CameraControlSwitcher controlSwitcher;
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
        StartCoroutine(WaitFrame());
    }
    public void HandleLevelScoutingFaze()
    {

        sceneMgr.Activate("ScoutingUI");
        currentlevelState = LevelState.Scouting;
        StartTrackOne();

        controlSwitcher.SwitchToCameraControl();
        GlobalInputManager.Instance.DisableControlSwapping();

        if (_playerSpawnObjects != null) _playerSpawnObjects.SpawningIsActive = false;
        else Debug.LogError("_playerSpawnObjects is null");
        if (_enemySpawnObjects != null) _enemySpawnObjects.SpawningIsActive = false;
        else Debug.LogError("_enemySpawnObjects is null");
        if (_timeMachanic != null) _timeMachanic.TimeIsActive = false;
        else Debug.LogError("_timeMachanic is null");
        if (_moneyMachanic != null) _moneyMachanic.MoneyIsActive = false;
        else Debug.LogError("_moneyMachanic is null");

    }

    public void OnEngaugeButtonPressed(InputAction.CallbackContext context)
    {
        //Debug.Log("OnEngaugeButtonPressed");
        if (currentlevelState != LevelState.Scouting || !context.performed) return;
        //check if they are sure
        // 3, 2, 1 go thing
        EngaugmentPartOne();
    }

    public void EngaugmentPartOne()
    {
        StopTrackOne();
        StartTrackTwo();

        currentlevelState = LevelState.EngaugmentPartOne;
        controlSwitcher.SwitchToPlayerControl();

        GlobalInputManager.Instance.EnableControlSwapping();
        GlobalInputManager.Instance.EnableCharacterSpawnControls();


        _playerSpawnObjects.SpawningIsActive = true;
        _enemySpawnObjects.SpawningIsActive = true;
        
        _timeMachanic.TimeIsActive = true;
        _moneyMachanic.MoneyIsActive = true;

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

    internal void StopTrackOne()
    {
        TrackfadeOutOne?.Invoke();
    }

    //extra
    IEnumerator WaitFrame()
    {
        yield return null;
        HandleLevelScoutingFaze();
    }
}
