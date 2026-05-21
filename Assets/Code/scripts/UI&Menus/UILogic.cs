using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class UILogic : MonoBehaviour
{
    [SerializeField] int SceneToLoad;
    [SerializeField] bool DebugLogs;
    [SerializeField] SceneActivityManager sceneMgr;

    public static bool GameIsPaused = false;
    public UnityEvent PauseEvent, ResumeEvent;
    //[SerializeField] bool DebugLogs = false;

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

        uiActions.TogglePause.performed += TogglePause;
        if(DebugLogs)Debug.Log("[UILogic] Subscribed to UI.TogglePause");
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var uiActions = GlobalInputManager.Instance.InputActions.UI;

        uiActions.TogglePause.performed -= TogglePause;
    }

    void Start()
    {
        if (sceneMgr == null)sceneMgr = FindFirstObjectByType<SceneActivityManager>();
        Debug.Assert(sceneMgr != null);
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }
        
    }

    public void TogglePause(InputAction.CallbackContext context)
    {
        if(DebugLogs) Debug.Log($"[UILogic] TogglePause called. performed: {context.performed}, GameIsPaused: {GameIsPaused}");
        if (!context.performed) return;

        if (GameIsPaused)
            Resume();
        else
            Pause();
    }

    public void Resume()
    {
        if(DebugLogs) Debug.Log($"Game is paused was {GameIsPaused} when trying to resume");
        if (GameIsPaused)
        {
            FModAudioManager.instance.PlaySoundByName("resumeGame");
            sceneMgr.ActivateAnchorSA();
            Time.timeScale = 1;
            GameIsPaused = false;
            
            // Return to appropriate mode when unpausing
            if (CameraControlSwitcher.Instance != null && CameraControlSwitcher.Instance.FreeCamIsActive) //this function is wrong it should store whatever the previous input setings were
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
        
        FModAudioManager.instance.PlaySoundByName("pauseGame");
        sceneMgr.Activate("Pause", false);
        Time.timeScale = 0;
        GameIsPaused = true;
        GlobalInputManager.Instance.SetPauseMenuMode();
        
        GlobalInputManager.Instance.EnableCursor();
        PauseEvent?.Invoke();
        if(DebugLogs) Debug.Log($"Game is paused was {GameIsPaused} when trying to pause");
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

    public void ButtonSound()
    {
        FModAudioManager.instance.PlaySoundByName("menuClick");
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