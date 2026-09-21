using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuUtilities
{
    public static void LoadMenu(int sceneIndex = 0)
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(sceneIndex);
    }

    public static void RestartCurrentScene()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
