using UnityEngine;

public class MainMenu : MonoBehaviour
{

   //for mouse click sound effect
   private AudioManager audio_manager;

   private void Start()
   {
      audio_manager = FindAnyObjectByType<AudioManager>();
   }

   


   public void QuitGame()
   {
      Application.Quit();
   }
}
