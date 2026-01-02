using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UILogic : MonoBehaviour
{
    [SerializeField] int SceneToLoad;

    AudioManager _audioManager;
    GameState gameState;
    SceneActivityManager sceneMgr;

    bool GameIsPaused = false;
    bool CharacterSelectIsOpen = false;
    bool MenuIsOpen = false;

    void Awake()
    {
        gameState = FindFirstObjectByType<GameState>();
        _audioManager = FindFirstObjectByType<AudioManager>();
    }

    void OnEnable()
    {
        // Subscribe to input from GlobalInputManager (with safety check)
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }
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
    }

    public void ClickSound()
    {
        _audioManager?.Play("Button Click");
    }

    public void ToggleCharacterSelect(InputAction.CallbackContext context)
    {
        //press 'c' to activate
        if (!context.performed || GameIsPaused || gameState.currentlevelState != GameState.LevelState.Scouting) 
            return;

        if (!CharacterSelectIsOpen)
        {
            sceneMgr.Activate("Loadout Select");
            CharacterSelectIsOpen = true;
            MenuIsOpen = true;
            
            // Switch to spawning mode when character select opens
            GlobalInputManager.Instance.SetSpawningMode();
        }
        else
        {
            sceneMgr.ActivatePreviousSA();
            CharacterSelectIsOpen = false;
            MenuIsOpen = false;
            
            // Return to gameplay mode when character select closes
            // Check if we're in freecam or player control mode
            if (CameraControlSwitcher.Instance != null && CameraControlSwitcher.Instance.FreeCamIsActive)
            {
                GlobalInputManager.Instance.SetFreeCamMode();
            }
            else
            {
                GlobalInputManager.Instance.SetGameplayMode();
            }
        }
        
        UpdateCursorState();
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
            MenuIsOpen = false;
            
            // Return to appropriate mode when unpausing
            if (CameraControlSwitcher.Instance != null && CameraControlSwitcher.Instance.FreeCamIsActive)
            {
                GlobalInputManager.Instance.SetFreeCamMode();
            }
            else
            {
                GlobalInputManager.Instance.SetGameplayMode();
            }
            
            UpdateCursorState();
        }
    }

    void Pause()
    {
        if (!GameIsPaused)
        {
            sceneMgr.Activate("Pause");
            Time.timeScale = 0;
            GameIsPaused = true;
            MenuIsOpen = true;
            
            // Switch to pause menu mode
            GlobalInputManager.Instance.SetPauseMenuMode();
            
            UpdateCursorState();
        }
    }

    void UpdateCursorState()
    {
        if (MenuIsOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            //Debug.Log("LockMouse -> Release Cursor");
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            //Debug.Log("LockMouse -> Lock Cursor");
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

    public void QuitGame() => Application.Quit();

    // Called by events when scene changes
    public void OnEventRaised()
    {
        UpdateCursorState();
    }
}