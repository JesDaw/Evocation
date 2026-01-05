using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void click_sound()
    {
        FModAudioManager.instance.PlayOneShot(FModEvents.instance.menuClick);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}