using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UILogic : MonoBehaviour
{   
    [SerializeField] int SceneToLoad;
    
    AudioManager _audioManager;
    PlayerInput playerInput;
    GameState gameState;
    SceneActivityManager sceneMgr;


    bool GameIsPaused = false;
    bool CharacterSelectIsOpen = false;
    bool MenuIsOpen = false;

    void Awake()
    {
        
        playerInput = GetComponent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogError($"[{name}] Missing PlayerInput component!");
            return;
        }
        gameState = FindFirstObjectByType<GameState>();
    }

    void Start()
    {
        sceneMgr = FindFirstObjectByType<SceneActivityManager>();
        Debug.Assert(sceneMgr != null);
    }
    public void ClickSound()
    {
        _audioManager.Play("Button Click");
    }

    public void ToggleCharacterSelect(InputAction.CallbackContext context)
    {
        //press 'c' to activate
        if(!context.performed || GameIsPaused || gameState.currentlevelState != GameState.LevelState.Scouting) return;

        if (!CharacterSelectIsOpen)
        {
            sceneMgr.Activate("new Loadout Select");
            CharacterSelectIsOpen = true;
            MenuIsOpen = true;
        }
        else
        {
            sceneMgr.ActivatePreviousSA();
            CharacterSelectIsOpen = false;
            MenuIsOpen = false;
        }
    }

    public void TogglePause(InputAction.CallbackContext context)
    {
        if(!context.performed) return;
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
    }

    public void LoadMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    public void LoadScene()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneToLoad);
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

