using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using Debug = UnityEngine.Debug;
using FMODUnity;
using FMOD.Studio;
using System.Dynamic;

public class GameState : MonoBehaviour
{
    // For controlling game mechanics

    //Game mechanic manager
    [SerializeField] Money _moneyMachanic;
    [SerializeField] Timer _timeMachanic;
    [SerializeField] PlayerStateMachine _playerStateMachine;
    [SerializeField] CameraControlSwitcher controlSwitcher;
    [SerializeField] SpawnObjects _playerSpawnObjects, _enemySpawnObjects;
    //cutscene manager
    [SerializeField] List<PlayableDirector> playableDirectors = new List<PlayableDirector>();
    [SerializeField] internal UltEvents.UltEvent LevelPartOne, LevelPartTwo, LevelPartThree;
    [SerializeField] internal UltEvents.UltEvent GameWin, GameLoose;

    public LevelState currentlevelState;

    SceneActivityManager sceneMgr;
    bool EngaugeButtonWasPressed = false;
    // Fmodaudio manager
    EventInstance ambienceEventInstance;
    EventInstance musicEventInstance;

    //controls manager

    public enum LevelState // other scripts use this enum in if statements to check if they are able to do certain actions at the current phase of the level
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

    enum MusicState
    {
        PART_1 = 0,
        PART_2 = 1,
        PART_3 = 2,
        LEVEL_WIN = 3
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

    // I think the global input manager has a single function that acomplishes this now. like we can just do GlobalInputManager.Instance.SetCutsceneMode();
    GlobalInputManager.Instance.DisableControlSwapping();
    GlobalInputManager.Instance.DisableCameraControls();

    // This probablu should be managed in another file. probably the same one as controlSwitcher.SwitchToCameraControl();
    if (_playerSpawnObjects != null) _playerSpawnObjects.SpawningIsActive = false;
    else Debug.LogError("_playerSpawnObjects is null");
    if (_enemySpawnObjects != null) _enemySpawnObjects.SpawningIsActive = false;
    else Debug.LogError("_enemySpawnObjects is null");
    if (_timeMachanic != null) _timeMachanic.TimeIsActive = false;
    else Debug.LogError("_timeMachanic is null");
    if (_moneyMachanic != null) _moneyMachanic.DeactivateMoney();
    else Debug.LogError("_moneyMachanic is null");
    
    // Initialize ambience using FModEvents
    if (FModEvents.instance != null)
    {
        InitializeAmbience(FModEvents.instance.ambiance);
        InitializeFModMusic(FModEvents.instance.music);
    }
    else
    {
        Debug.LogError("FModEvents.instance is null! Make sure FModEvents GameObject exists in the scene.");
    }
 
    
    HandleLevelIntro();
}

    void InitializeAmbience(EventReference ambienceEventReference)
    {
        // Create the instance directly using RuntimeManager
        ambienceEventInstance = RuntimeManager.CreateInstance(ambienceEventReference);
        ambienceEventInstance.start();
    }
    void InitializeFModMusic(EventReference musicEventReference)
    {
        // Create the instance and assign it to the class field musicEventInstance
        musicEventInstance = RuntimeManager.CreateInstance(musicEventReference);
    }

    void OnDestroy()
    {
        // Clean up the ambience instance when this object is destroyed
        if (ambienceEventInstance.isValid())
        {
            ambienceEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            ambienceEventInstance.release();
        }
    }

    //=======================LEVEL START=======================================
    internal void HandleLevelIntro()
    {
        currentlevelState = LevelState.Intro;
        
        GlobalInputManager.Instance.SetCutsceneMode();
        StartCoroutine(WaitFrame());
        
    }
    IEnumerator WaitFrame() //cutscenes should be managed in their own seprate file
    {
        yield return null;
        if (playableDirectors.Count > 0)
        {
            if(playableDirectors[0].state != PlayState.Playing)
            {
                HandleInGameCutscene(0);
            }
            
        }
        else
        {
            Debug.LogWarning($"No PlayableDirectors in playableDirectors list");
        }
    }

    public void HandleLevelScoutingFaze()
    {
        Time.timeScale = 1; // I think time scale should be managed in the game mechanics manager
        currentlevelState = LevelState.Scouting;
        sceneMgr.Activate("ScoutingUI");
        SetMusicSection(MusicState.PART_1);
        musicEventInstance.start();
        
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
    public void BackToScouting() //could just merge this into HandleLevelScoutingFaze() and just use that one function.
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
        sceneMgr.Activate("GamePlayUI", makeAnchor: true);
        SetMusicSection(MusicState.PART_2);

        currentlevelState = LevelState.EngaugmentPartOne;

        GlobalInputManager.Instance.SetPlayerCharacterMode();
        
        _playerSpawnObjects.SpawningIsActive = true; // I think this is already handles by the spawning controls being disabled

        //this should all be handles in the game systems manager
        controlSwitcher.SwitchToPlayerControl();
        _enemySpawnObjects.SpawningIsActive = true; // Enable enemy cpu spawning
        _timeMachanic.TimeIsActive = true; // dont I have functions for this? I dont think these need to be manipulated directly
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
        SetMusicSection(MusicState.PART_3);
        LevelPartThree?.Invoke();
    }

    // ===========================================LEVEL END============================================
    public void HandleLevelWin()
    {
        currentlevelState = LevelState.Win;


        _moneyMachanic.DeactivateMoney();
        _timeMachanic.DeactivateTimer();
        SetMusicSection(MusicState.LEVEL_WIN);

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

        //should be in game systems manager script
        _moneyMachanic.DeactivateMoney();
        _timeMachanic.DeactivateTimer();

        if (playableDirectors.Count > 3) // I should use enums to represent numbers instead of just numbers to label each timeline, like how I have it set up with the FModAudio, the numbers and confusing
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

    internal void HandleInGameCutscene(int director)
    {
        sceneMgr.Activate("Blank UI");
        
        // Set cutscene mode when playing any cutscene
        GlobalInputManager.Instance.SetCutsceneMode();
        
        playableDirectors[director].Play();
        //Debug.Log($"Playing {playableDirectors[director]}");
    }

    
    // random comment
    void SetMusicSection(MusicState state)
    {
        UnityEngine.Debug.Log($"Changing music to {state}");
        musicEventInstance.setParameterByName("MountainLevelPhases", (float)state);
    }
}