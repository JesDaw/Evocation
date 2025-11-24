using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class GameState : MonoBehaviour
{
    // For controlling game mechanics
    [SerializeField] Money _moneyMachanic;
    [SerializeField] Timer _timeMachanic;
    [SerializeField] PlayerStateMachine _playerStateMachine;
    [SerializeField] CameraControlSwitcher controlSwitcher;
    [SerializeField] SpawnObjects _playerSpawnObjects, _enemySpawnObjects;
    [SerializeField] List<PlayableDirector> playableDirectors = new List<PlayableDirector>();
    [SerializeField] internal UltEvents.UltEvent LevelPartOne, LevelPartTwo, LevelPartThree;
    
    // Music
    [SerializeField] internal UltEvents.UltEvent TrackfadeInOne, TrackfadeInTwo, TrackfadeInThree;
    [SerializeField] internal UltEvents.UltEvent TrackfadeOutOne, TrackfadeOutTwo, TrackfadeOutThree;
    [SerializeField] internal UltEvents.UltEvent GameWin, GameLoose;
    
    public LevelState currentlevelState;
    InputAction engaugeAction, toggleCharacterSeceltAction;
    PlayerInput playerInput;

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
        playerInput = GetComponent<PlayerInput>();
        foreach (PlayableDirector director in playableDirectors)
        {
            if (director != null)
            {
                director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            }
        }
    }

    void Start()
    {
        // Find the SceneActivityManager
        foreach (var obj in Resources.FindObjectsOfTypeAll<SceneActivityManager>())
        {
            sceneMgr = obj;
        }
        Debug.Assert(sceneMgr != null);

        // Forces free cam mode
        controlSwitcher.SwitchToCameraControl();
        GlobalInputManager.Instance.DisableControlSwapping();
        GlobalInputManager.Instance.DisableCameraControls();

        // Disables other parts of game
        if (_playerSpawnObjects != null) _playerSpawnObjects.SpawningIsActive = false;
        else Debug.LogError("_playerSpawnObjects is null");
        if (_enemySpawnObjects != null) _enemySpawnObjects.SpawningIsActive = false;
        else Debug.LogError("_enemySpawnObjects is null");
        if (_timeMachanic != null) _timeMachanic.TimeIsActive = false;
        else Debug.LogError("_timeMachanic is null");
        if (_moneyMachanic != null) _moneyMachanic.DeactivateMoney();
        else Debug.LogError("_moneyMachanic is null");

        HandleLevelIntro();
    }

    //=======================LEVEL START=======================================
    internal void HandleLevelIntro()
    {
        currentlevelState = LevelState.Intro;
        if(playableDirectors.Count > 0)
        {
            HandleInGameCutscene(0);
        }
        else
        {
            Debug.LogWarning($"No PlayableDirectors in playableDirectors list");
        }
    }

    public void OnIntroCutsceneFinishedTest(InputAction.CallbackContext context)
    {
        //Debug.Log("OnIntroCutsceneFinishedTest button pressed");
        if (!context.performed) return;
        OnIntroCutsceneFinished();
    }

    internal void OnIntroCutsceneFinished()
    {
        if (currentlevelState == LevelState.Intro) HandleLevelScoutingFaze();
    }

    public void HandleLevelScoutingFaze()
    {
        Time.timeScale = 1;
        currentlevelState = LevelState.Scouting;
        sceneMgr.Activate("ScoutingUI");
        StartTrackOne(); 
        GlobalInputManager.Instance.EnableCameraControls();
    }

    public void OnEngaugeButtonPressed(InputAction.CallbackContext context)
    {        
        if (currentlevelState != LevelState.Scouting || !context.performed) return;
        
        if(playableDirectors.Count > 1)
        {
            // 3, 2, 1 go cutscene
            HandleInGameCutscene(1);
        }
        else
        {
            EngaugmentPartOne();
        }
    }

    //================================================GAMEPLAY========================================

    public void EngaugmentPartOne()
    {
        sceneMgr.Activate("GamePlayUI");
        StopTrackOne();
        StartTrackTwo();
        
        currentlevelState = LevelState.EngaugmentPartOne;

        // Enable global shared controls
        GlobalInputManager.Instance.EnableCameraControls();
        GlobalInputManager.Instance.EnableControlSwapping();
        GlobalInputManager.Instance.EnableCharacterSpawnControls();

        // Switch to player control (this will enable the active player's inputs)
        controlSwitcher.SwitchToPlayerControl();
        
        //Debug.Log("EngaugmentPartOne: Player control should now be active");

        // Enable spawning
        _playerSpawnObjects.SpawningIsActive = true;
        _enemySpawnObjects.SpawningIsActive = true;
        
        _timeMachanic.TimeIsActive = true;
        _moneyMachanic.ActivateMoney();

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

    // ===========================================LEVEL END============================================
    public void HandleLevelWin()
    {
        currentlevelState = LevelState.Win;
        _moneyMachanic.DeactivateMoney();
        _timeMachanic.DeactivateTimer();
        
        if(playableDirectors.Count > 2)
        {
            HandleInGameCutscene(2);
        }
        else
        {
            sceneMgr.Activate("Victory");
            Time.timeScale = 0;
            Debug.LogWarning($"No 'Win' PlayableDirector in playableDirectors list");
        }
    }

    public void HandleLevelLoss()
    {
        currentlevelState = LevelState.Loose;
        _moneyMachanic.DeactivateMoney();
        _timeMachanic.DeactivateTimer();
        
        if(playableDirectors.Count > 3)
        {
            HandleInGameCutscene(3);
        }
        else
        {
            sceneMgr.Activate("Defeat");
            Time.timeScale = 0;
            Debug.LogWarning($"No 'Loose' PlayableDirector in playableDirectors list");
        }
    }

    //===================================Music tracks=======================================
    internal void HandleInGameCutscene(int director)
    {
        Time.timeScale = 0;
        playableDirectors[director].Play();
    }
    
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
}