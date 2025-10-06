using UnityEngine;

public class MainMenu : MonoBehaviour
{

   //for mouse click sound effect
   private AudioManager audio_manager;

   private void Start()
   {
      audio_manager = FindAnyObjectByType<AudioManager>();
   }

   public void click_sound()
   {
      audio_manager.Play("Button Click"); //play this sfx when a menu UI button is clicked
   }

   public void QuitGame()
   {
      Application.Quit();
   }
}
