using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField]  bool GameIsPaused = false;
    [SerializeField]  bool GameIsOver = false;
    public GameObject pauseMenuUI;
    [SerializeField] UnityEvent ToggleMenu;
    [SerializeField] UnityEvent _ResetValues;
    [SerializeField] GameObject _settingsMenu;



    InputAction pauseAction;

    void Start()
    {
        pauseAction = InputSystem.actions.FindAction("Pause");
    }

    public void GameOver(){ GameIsOver = !GameIsOver;}

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
        if(context.performed && GameIsOver == false)
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
        if (_settingsMenu.activeSelf) _settingsMenu.SetActive(false);
        Debug.Log("resumed");
        ToggleMenu.Invoke();
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1;
        GameIsPaused = false;
    }

    void Pause()
    {
        ToggleMenu.Invoke();
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0;
        GameIsPaused = true;
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

    void ResetValues(){ _ResetValues.Invoke(); }
}
