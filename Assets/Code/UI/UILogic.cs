using System;
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
    //public static UnityEvent PauseEvent, ResumeEvent;
    //[SerializeField] bool DebugLogs = false;
    public static float SpellTimeScaleRequest = 1f;
    
    [Flags]
    public enum PauseState
    {
        Unpaused = 0,
        MenuPaused = 1,
        SpellPaused = 2,
    }

    public static PauseState pauseState = PauseState.Unpaused;

    void OnEnable()
    {
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }        
    }

    void OnDisable()
    {
        GameIsPaused = false;
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
            // Time.timeScale = 1;
            toggleMenuPaused();
            GameIsPaused = false;
            GlobalInputManager.Instance.PopMode();
            GlobalInputManager.Instance.DisableCursor();
        }
    }

    void Pause()
    {
        
        FModAudioManager.instance.PlaySoundByName("pauseGame");
        sceneMgr.Activate("Pause", false);
        // Time.timeScale = 0;
        toggleMenuPaused();
        GameIsPaused = true;
        GlobalInputManager.Instance.PushCurrentMode();
        GlobalInputManager.Instance.SetMode(InputMode.PauseMenu);
        
        GlobalInputManager.Instance.EnableCursor();
        if(DebugLogs) Debug.Log($"Game is paused was {GameIsPaused} when trying to pause");
    }
    
    public static void RequestSpellTimeScale(float scale)
    {
        SpellTimeScaleRequest = Mathf.Max(scale, 0f);
        ApplyEffectiveTimeScale();
    }

    public static void ClearSpellTimeScale()
    {
        SpellTimeScaleRequest = 1f;
        ApplyEffectiveTimeScale();
    }

    static void ApplyEffectiveTimeScale()
    {
        float scale = (pauseState & PauseState.MenuPaused) != 0 ? 0f : SpellTimeScaleRequest;
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * Mathf.Max(scale, 0.0001f);
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
    
    void toggleMenuPaused()
    {
        pauseState ^= PauseState.MenuPaused;
        ApplyEffectiveTimeScale();
    }
}