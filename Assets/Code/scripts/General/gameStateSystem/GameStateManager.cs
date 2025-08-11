using UnityEngine;

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
    GameStateCaller _previousRootState;
    GameStateCaller _currentRootState;

    void Start()
    {
        _timeScale = 1;
        if (_timer == null) Debug.LogWarning($"Game state manager doesnt have timer object attached so cant interact with it");
        if (_money == null) Debug.LogWarning($"Game state manager doesnt have money object attached so cant interact with it");
    }

    public void GameStateChanger(GameStateCaller state)
    {
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
        if (state.CutscenToActivate != null)
        {
            state.CutscenToActivate.Play();
        }
    }
}
