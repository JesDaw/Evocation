using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour
{
   public void PlayGame()
   {
       //SceneManager.LoadScene("Level 1");
        SceneManager.LoadScene("Week 1 stuff");
   }
   public void QuitGame()
   {
      Application.Quit();
   }
}
