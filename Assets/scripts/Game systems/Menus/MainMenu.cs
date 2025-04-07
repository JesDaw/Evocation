using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

   //for mouse click sound effect
   private AudioManager audio_manager;

   private void Start()
   {
      audio_manager = FindAnyObjectByType<AudioManager>();
   }

   public void PlayGame()
   {
        //SceneManager.LoadScene("Level 1");
        //SceneManager.LoadScene("Week 3 stuff");

        //for testing if audio changes on a different scene
        SceneManager.LoadScene("Week 6 stuff");
   }

   public void click_sound()
   {
      audio_manager.Play("Button click"); //play this sfx when a menu UI button is clicked
   }


   public void QuitGame()
   {
      Application.Quit();
   }
}
