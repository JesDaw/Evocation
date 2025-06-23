using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class GameState : MonoBehaviour
{
    [SerializeField] bool GameIsPaused = false;
    [SerializeField] bool GameIsOver = false;
    [SerializeField] UnityEvent _ResetValues;

    InputAction pauseAction;

    SceneActivityManager sceneMgr;

    void Start()
    {
        pauseAction = InputSystem.actions.FindAction("Pause");

        // Find the SceneActivityManager!
        foreach (var obj in Resources.FindObjectsOfTypeAll<SceneActivityManager>())
        {
            sceneMgr = obj;
        }
        Debug.Assert(sceneMgr != null);
    }

    public void HandleGameWin()
    {
        GameIsOver = true;
        sceneMgr.Activate("Victory");
        Time.timeScale = 0;
    }

    public void HandleGameLoss()
    {
        GameIsOver = true;
        sceneMgr.Activate("Defeat");
        Time.timeScale = 0;
    }

    private void OnEnable()
    {
        if (pauseAction != null)
            pauseAction.Enable();
    }

    private void OnDisable()
    {
        if (pauseAction != null)
            pauseAction.Disable();
    }

    public void TogglePause(InputAction.CallbackContext context)
    {
        if (context.performed && GameIsOver == false)
        {
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

    public void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        ResetValues();

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
