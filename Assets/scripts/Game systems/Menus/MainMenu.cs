using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour
{
   public void PlayGame()
   {
       //SceneManager.LoadScene("Level 1");
      //SceneManager.LoadScene("Week 3 stuff");

      //for testing if audio changes on a different scene
      SceneManager.LoadScene("Emi - W4 Sound");
   }
   public void QuitGame()
   {
      Application.Quit();
   }
}
