using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UILogic : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] PlayerInput playerInput;

    [SerializeField] GameState gameState;
    [SerializeField] UnityEvent _ResetValues;

    [SerializeField] int SceneToLoad;
    [SerializeField] bool MenuIsOpen = false;
    AudioManager _audioManager;

    SceneActivityManager sceneMgr;
    bool GameIsPaused = false;
    bool CharacterSelectIsOpen = false;

    void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogError($"[{name}] Missing PlayerInput component!");
            return;
        }

        if (gameState == null)
            gameState = FindFirstObjectByType<GameState>();
    }

    void Start()
    {
        sceneMgr = FindFirstObjectByType<SceneActivityManager>();
        Debug.Assert(sceneMgr != null);
    }

    // This gets called automatically when the PlayerInput triggers a "performed" event
    public void ESCpressed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        Debug.Log("ESC pressed");

        if (gameState.currentlevelState == GameState.LevelState.Scouting)
        {
            ToggleCharacterSelect();
        }
        else
        {
            TogglePause();
        }
    }

    public void ClickSound()
    {
        _audioManager.Play("Button Click");
    }

    void ToggleCharacterSelect()
    {
        Debug.Log($"{CharacterSelectIsOpen}");

        if (!CharacterSelectIsOpen)
        {
            Debug.Log("activating loadout");
            sceneMgr.Activate("Loadout Select");
        }
        else
        {
            Debug.Log("activating ScoutingUI");
            sceneMgr.Activate("ScoutingUI");
        }

        CharacterSelectIsOpen = !CharacterSelectIsOpen;
        MenuIsOpen = !MenuIsOpen;
    }

    void TogglePause()
    {
        if (GameIsPaused)
            Resume();
        else
            Pause();
    }

    public void Resume()
    {
        if (GameIsPaused)
        {
            sceneMgr.ActivateInitialSA();
            Time.timeScale = 1;
            GameIsPaused = false;
            MenuIsOpen = false;
        }
    }

    void Pause()
    {
        if (!GameIsPaused)
        {
            sceneMgr.Activate("Pause");
            Time.timeScale = 0;
            GameIsPaused = true;
            MenuIsOpen = true;
        }
    }

    public void ReloadCurrentScene()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        _ResetValues?.Invoke();
    }

    public void LoadMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
        _ResetValues?.Invoke();
    }

    public void LoadScene()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneToLoad);
        _ResetValues?.Invoke();
    }

    public void QuitGame() => Application.Quit();


    public void OnEventRaised()
    {
        bool isMenuCurrentlyOpen = !sceneMgr.InInitialSA();

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

