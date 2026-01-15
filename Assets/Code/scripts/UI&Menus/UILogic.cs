using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class UILogic : MonoBehaviour
{
    [SerializeField] int SceneToLoad;
    AudioManager _audioManager;
    SceneActivityManager sceneMgr;

    public static bool GameIsPaused = false;
    bool CharacterSelectIsOpen = false;
    public UnityEvent PauseEvent, ResumeEvent;
    [SerializeField] bool DebugLogs = false;

    void Awake()
    {
        _audioManager = FindFirstObjectByType<AudioManager>();
    }

    void OnEnable()
    {
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }        
    }

    void OnDisable()
    {
        UnsubscribeFromInputs();
    }

    void SubscribeToInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var uiActions = GlobalInputManager.Instance.InputActions.UI;
        
        uiActions.ToggleCharacterSelect.performed += ToggleCharacterSelect;
        uiActions.TogglePause.performed += TogglePause;
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var uiActions = GlobalInputManager.Instance.InputActions.UI;
        
        uiActions.ToggleCharacterSelect.performed -= ToggleCharacterSelect;
        uiActions.TogglePause.performed -= TogglePause;
    }

    void Start()
    {
        sceneMgr = FindFirstObjectByType<SceneActivityManager>();
        Debug.Assert(sceneMgr != null);
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }
        
    }

    public void ClickSound() // this should be in the audio manager
    {
        FModAudioManager.instance.PlayOneShot(FModEvents.instance.menuClick);
    }

    public void ToggleCharacterSelect(InputAction.CallbackContext context)
    {
        if (DebugLogs)Debug.Log($"character select pressed");
        if (!context.performed || GameIsPaused || !LevelStateManager.Instance.IsInState("ScoutingState"))
        {
            return;
        }


        if (!CharacterSelectIsOpen)
        {
            sceneMgr.Activate("LoadoutSelectUI", true);
            CharacterSelectIsOpen = true;
            
            GlobalInputManager.Instance.SetCharacterSelectingMode();
            GlobalInputManager.Instance.EnableCursor();
        }
        else
        {
            sceneMgr.Activate("ScoutingUI", true);
            CharacterSelectIsOpen = false;
            GlobalInputManager.Instance.DisableCursor();
            if (CameraControlSwitcher.Instance != null && CameraControlSwitcher.Instance.FreeCamIsActive)
            {
                GlobalInputManager.Instance.SetFreeCamMode();
            }
            else
            {
                GlobalInputManager.Instance.SetPlayerCharacterMode();
            }
        }
    }

    public void TogglePause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        if (GameIsPaused)
            Resume();
        else
            Pause();
    }

    public void Resume()
    {
        if (GameIsPaused)
        {
            sceneMgr.ActivateAnchorSA();
            Time.timeScale = 1;
            GameIsPaused = false;
            
            // Return to appropriate mode when unpausing
            if (CameraControlSwitcher.Instance != null && CameraControlSwitcher.Instance.FreeCamIsActive)
            {
                GlobalInputManager.Instance.SetFreeCamMode();
            }
            else
            {
                GlobalInputManager.Instance.SetPlayerCharacterMode();
            }
            
            GlobalInputManager.Instance.DisableCursor();
            ResumeEvent?.Invoke();
        }
    }

    void Pause()
    {
        if (!GameIsPaused)
        {
            sceneMgr.Activate("Pause");
            Time.timeScale = 0;
            GameIsPaused = true;
            
            GlobalInputManager.Instance.SetPauseMenuMode();
            
            GlobalInputManager.Instance.EnableCursor();
            PauseEvent?.Invoke();
        }
    }

    public void ReloadCurrentScene()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    public void LoadScene()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneToLoad);
    }

    public void QuitGame() 
    { 
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}