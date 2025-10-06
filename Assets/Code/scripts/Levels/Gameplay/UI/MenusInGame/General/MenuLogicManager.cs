using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MenuLogicManager : MonoBehaviour
{
    [SerializeField] bool MenuIsOpen;
    [SerializeField] UnityEvent _ResetValues;
    [SerializeField] int SceneToLoad;

    SceneActivityManager sceneMgr;
    AudioManager audio_manager;

    void Awake()
    {
        MenuIsOpen = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Find the SceneActivityManager!
        foreach (var obj in Resources.FindObjectsOfTypeAll<SceneActivityManager>())
        {
            sceneMgr = obj;
        }
        Debug.Assert(sceneMgr != null);
        audio_manager = FindAnyObjectByType<AudioManager>();
    }

    public void click_sound()
    {
        audio_manager.Play("Button Click"); 
    }

    public void LoadScene()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneToLoad);
        ResetValues();
    }

    public void ReloadCurrentScene()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        ResetValues();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    void ResetValues()
    {
        _ResetValues.Invoke();
    }

    public void OnEventRaised()
    {
        //this is auto called when menus get activated or deactivated
        // The Initial SceneActivity for this Scene is the
        // GamePlayUI.  If we are in any other SA then we
        // are in some kind of Menu!
        bool isMenuCurrentlyOpen = !sceneMgr.InInitialSA();

        if (MenuIsOpen != isMenuCurrentlyOpen)
        {
            // Update our state
            MenuIsOpen = isMenuCurrentlyOpen;

            if (MenuIsOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                Debug.Log("LockMouse -> Release Cursor");
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                Debug.Log("LockMouse -> Lock Cursor");
            }
        }
    }
}
