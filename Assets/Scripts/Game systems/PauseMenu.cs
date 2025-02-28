using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public static bool GameIsOver = false;
    public GameObject pauseMenuUI;
    public GameObject victoryMenuUI;
    public GameObject defeatMenuUI;

    void Update()
    {
        if (GameIsOver == false)
        {
            victoryMenuUI.SetActive(false);
            defeatMenuUI.SetActive(false);
        }
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
        if (Input.GetKeyDown(KeyCode.K))
        {
            victoryMenuUI.SetActive(true);
            GameIsOver = true;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            defeatMenuUI.SetActive(true);
            GameIsOver = true;
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1;
        GameIsPaused = false;
    }

    void Pause()
    {
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
