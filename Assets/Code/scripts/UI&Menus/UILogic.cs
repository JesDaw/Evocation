using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class UILogic : MonoBehaviour
{
    InputAction pauseAction;
    [SerializeField] private GameState gameState;
    [SerializeField] UnityEvent _ResetValues;
    SceneActivityManager sceneMgr;
    bool GameIsPaused = false;
    void Awake()
    {
        pauseAction = InputSystem.actions.FindAction("Pause");
        if (gameState == null)
        {
            gameState = FindFirstObjectByType<GameState>();

            if (gameState != null)
            {
                Debug.LogWarning($"[{name}] Auto-assigned GameState via FindFirstObjectByType.", this);
            }
            else
            {
                Debug.LogError($"[{name}] Missing GameState reference! Please assign manually.", this);
            }
        }
    }
    void OnEnable()
    {
        if (pauseAction != null) pauseAction.Enable();
    }
    private void OnDisable()
    {
        if (pauseAction != null) pauseAction.Disable();
    }
    void Start()
    {
        foreach (var obj in Resources.FindObjectsOfTypeAll<SceneActivityManager>())
        {
            sceneMgr = obj;
        }
        Debug.Assert(sceneMgr != null);
    }
    public void TogglePause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
                if (gameState.currentlevelState != GameState.LevelState.EngaugmentPartOne &&
                gameState.currentlevelState != GameState.LevelState.EngaugmentPartTwo &&
                gameState.currentlevelState != GameState.LevelState.EngaugmentPartThree)
                return;
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }
    public void Resume()
    {
        if (GameIsPaused)
        {
            Debug.Log("resumed");
            sceneMgr.ActivateInitialSA();

            Time.timeScale = 1;
            GameIsPaused = false;
        }
    }
    void Pause()
    {
        if (!GameIsPaused)
        {
            GameIsPaused = true;
            sceneMgr.Activate("Pause");
            Time.timeScale = 0;
        }
    }
    public void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        ResetValues();

    }


    public void LoadMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
        ResetValues();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    
        void ResetValues()
    {
        _ResetValues.Invoke();
    }
}
