using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public static bool GameIsOver = false;
    public GameObject pauseMenuUI;
    [SerializeField] UnityEvent ToggleMenu;

    InputAction pauseAction;

    void Start()
    {
        pauseAction = InputSystem.actions.FindAction("Pause");
    }

    public void GameOver(){ GameIsOver = !GameIsOver;}

    public void TogglePause(InputAction.CallbackContext context)  
    {
        if(context.performed & GameIsOver == false)
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

 /*  void Update()
    {
    if (Input.GetKeyDown(KeyCode.Escape) & GameIsOver == false)
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
*/
    public void Resume()
    {
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
        SceneManager.LoadScene(1);
        Time.timeScale = 1;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
