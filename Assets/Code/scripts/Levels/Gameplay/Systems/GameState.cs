using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using Debug = UnityEngine.Debug;

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

    SceneActivityManager sceneMgr;
    bool EngaugeButtonWasPressed = false;

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
        foreach (PlayableDirector director in playableDirectors)
        {
            if (director != null)
            {
                director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            }
        }
    }

    void OnEnable()
    {
        // Subscribe to the engage button (with safety check)

    }

    void OnDisable()
    {
        // Unsubscribe when disabled
        UnsubscribeFromInputs();
    }

    void SubscribeToInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var uiActions = GlobalInputManager.Instance.InputActions.UI;
        uiActions.StartEngaugment.performed += OnEngaugeButtonPressed;
        uiActions.Return.performed += BackToScouting;
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var uiActions = GlobalInputManager.Instance.InputActions.UI;
        uiActions.StartEngaugment.performed -= OnEngaugeButtonPressed;
        uiActions.Return.performed -= BackToScouting;
    }

    void Start()
    {
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }

        // Find the SceneActivityManager
        foreach (var obj in Resources.FindObjectsOfTypeAll<SceneActivityManager>())
        {
            sceneMgr = obj;
        }
        Debug.Assert(sceneMgr != null);

        // Forces free cam mode with disabled controls during intro
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
        
        // Set cutscene mode - only UI controls (for skipping if implemented)
        GlobalInputManager.Instance.SetCutsceneMode();
        
        if (playableDirectors.Count > 0)
        {
            HandleInGameCutscene(0);
        }
        else
        {
            Debug.LogWarning($"No PlayableDirectors in playableDirectors list");
        }
    }

    public void HandleLevelScoutingFaze()
    {
        Time.timeScale = 1;
        currentlevelState = LevelState.Scouting;
        sceneMgr.Activate("ScoutingUI");
        StartTrackOne();
        
        // Enable freecam mode for scouting
        GlobalInputManager.Instance.SetScoutingMode();
    }

    public void OnEngaugeButtonPressed(InputAction.CallbackContext context)
    {
        if (currentlevelState != LevelState.Scouting || !context.performed) return;
        if (!EngaugeButtonWasPressed){
            sceneMgr.Activate("ConfirmationUI");   
            GlobalInputManager.Instance.SetCharacterSelectingMode();
            EngaugeButtonWasPressed = true;
        }
        else
        {
            StartEngaugmentPartOne();
        }
    }

    public void BackToScouting(InputAction.CallbackContext context)
    {
        if (currentlevelState != LevelState.Scouting || !context.performed || !EngaugeButtonWasPressed) return;
        BackToScouting();
    }
    public void BackToScouting()
    {
        sceneMgr.Activate("ScoutingUI");
        GlobalInputManager.Instance.SetScoutingMode();
        EngaugeButtonWasPressed = false;
    }

    public void StartEngaugmentPartOne()
    {
        if (playableDirectors.Count > 1)
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
        Time.timeScale = 1;
        UnityEngine.Debug.Log("EngaugmentPartOne starting");
        sceneMgr.Activate("GamePlayUI", makeAnchor: true);
        StopTrackOne();
        StartTrackTwo();

        currentlevelState = LevelState.EngaugmentPartOne;

        // Use the gameplay mode preset which enables:
        // - Player controls
        // - Camera switching
        // - Player switching
        // - Spawner controls
        // - UI controls (pause menu)
        GlobalInputManager.Instance.SetPlayerCharacterMode();

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
        StopTrackTwo();
        StartTrackThree();
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

        if (playableDirectors.Count > 2)
        {
            // Set cutscene mode for win cutscene
            GlobalInputManager.Instance.SetCutsceneMode();
            HandleInGameCutscene(2);
        }
        else
        {
            // Disable all controls except UI menu navigation
            GlobalInputManager.Instance.SetPauseMenuMode();
            sceneMgr.Activate("Victory");
            Time.timeScale = 0;
            Debug.LogWarning($"No 'Win' PlayableDirector in playableDirectors list");
        }
        
        GameWin?.Invoke();
    }

    public void HandleLevelLoss()
    {
        currentlevelState = LevelState.Loose;
        _moneyMachanic.DeactivateMoney();
        _timeMachanic.DeactivateTimer();

        if (playableDirectors.Count > 3)
        {
            // Set cutscene mode for loss cutscene
            GlobalInputManager.Instance.SetCutsceneMode();
            HandleInGameCutscene(3);
        }
        else
        {
            // Disable all controls except UI menu navigation
            GlobalInputManager.Instance.SetPauseMenuMode();
            sceneMgr.Activate("Defeat");
            Time.timeScale = 0;
            Debug.LogWarning($"No 'Loose' PlayableDirector in playableDirectors list");
        }
        
        GameLoose?.Invoke();
    }

    //===================================Music tracks=======================================
    internal void HandleInGameCutscene(int director)
    {
        Time.timeScale = 0;
        sceneMgr.Activate("Blank UI");
        
        // Set cutscene mode when playing any cutscene
        GlobalInputManager.Instance.SetCutsceneMode();
        
        playableDirectors[director].Play();
        //Debug.Log($"Playing {playableDirectors[director]}");
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

    internal void StopTrackTwo()
    {
        TrackfadeOutTwo?.Invoke();
    }

    internal void StopTrackThree()
    {
        TrackfadeOutThree?.Invoke();
    }
}