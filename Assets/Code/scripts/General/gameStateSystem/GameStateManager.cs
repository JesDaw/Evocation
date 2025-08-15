using UnityEngine;
using UnityEngine.InputSystem;

public class GameStateManager : MonoBehaviour
{
    [Header("Game Objects to control")]
    [SerializeField] Timer _timer;
    [SerializeField] Money _money;

    [Header("There this is a desplay for debugging dont touch")]
    [SerializeField] float _timeScale = 1;
    [SerializeField] bool _timerStopped;
    [SerializeField] bool _moneyGenStopped;
    [SerializeField] bool _MoneySpendingStopped;
    [SerializeField] bool _allCharactersIdle;
    [SerializeField] bool GameIsPaused = false;
    [SerializeField] bool GameIsOver = false;
    GameStateCaller _previousRootState;
    GameStateCaller _currentRootState;
    SceneActivityManager sceneMgr;


    void Start()
    {
        _timeScale = 1;
        if (_timer == null) Debug.LogWarning($"Game state manager doesnt have timer object attached so cant interact with it");
        if (_money == null) Debug.LogWarning($"Game state manager doesnt have money object attached so cant interact with it");

        // Find the SceneActivityManager!
        sceneMgr = FindAnyObjectByType<SceneActivityManager>();
        Debug.Assert(sceneMgr != null);
    }


    public void GameStateChanger(GameStateCaller state)
    {
        if (state.PauseGame && GameIsPaused)
        {
            Debug.LogWarning($"{state.gameObject.name} is trying to pause the game but the game is already paused");
            return;   
        }
        if (!state.PauseGame && !GameIsPaused)
        {
            Debug.LogWarning($"{state.gameObject.name} is trying to unpause the game but the game already isn't paused");
            return;
        }
        GameIsPaused = state.PauseGame;

        Debug.Log($"Activating State: {state.StateName}");

        _previousRootState = _currentRootState;
        _currentRootState = state;


        Time.timeScale = state.TimeScale;
        _timeScale = state.TimeScale;
        if (state.EditTimer && state.StopTimer)
        {
            _timerStopped = state.StopTimer;
        }
        if (state.EditMoneySpending && state.StopMoneySpending)
        {
            _moneyGenStopped = state.StopMoneySpending;
        }
        if (state.EditMoneyGen && state.StopMoneySpending)
        {
            _MoneySpendingStopped = state.StopMoneySpending;
        }
        if (state.EditCharacterBehavior)
        {
            _allCharactersIdle = state.MakeAllCharactersIdle;
            if (state.KillAllFriends)
            {

            }
            if (state.KillAllFoes)
            {

            }
        }
        if (state.ObjectsToEnable.Count > 0)
        {
            foreach (GameObject obj in state.ObjectsToEnable)
            {
                Debug.Log($"enabling {obj.name}");
                obj.SetActive(true);
            }
        }
        if (state.ObjectsToDisable.Count > 0)
        {
            foreach (GameObject obj in state.ObjectsToDisable)
            {
                Debug.Log($"Dissabling {obj.name}");
                obj.SetActive(false);
            }
        }
        if (state.ManageUI)
        {
            if (state.UIToActivate != null) sceneMgr.Activate(state.UIToActivate);
            if (state.ActivateInitialSA) sceneMgr.ActivateInitialSA();
            if (state.ActivateCursor)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Debug.Log("GamestateManager -> Activated Cursor");
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Debug.Log("LockMouse -> Release Cursor");
            }
            if (state.ManageUI &&!state.PauseGame)
            {
                sceneMgr.ActivateInitialSA();
            }

        }
        if (state.CutscenToActivate != null)
        {
            state.CutscenToActivate.Play();
        }
    }
}
