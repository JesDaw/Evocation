using Unity.Cinemachine;
using UnityEngine;

public class ActivePlayer : MonoBehaviour
{
    public delegate void OnPlayerDeactivating(GameObject player);

    /// <summary>
    /// This event is sent just before a new player is activated
    /// </summary>
    public event OnPlayerDeactivating PlayerDeactivating;

    public delegate void OnPlayerActivating(GameObject player);
    /// <summary>
    /// This event is sent just after a new player is activated
    /// </summary>
    public event OnPlayerActivating PlayerActivating;

    [SerializeField] GameObject _currentPlayer;
    public static ActivePlayer Instance { get; private set; }
    GameObject _currentCamera;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Removed the GlobalInputManager.SetActivePlayer() call
    // PlayerSwitch now handles activating player inputs directly

    public GameObject CurrentPlayer
    {
        get { return _currentPlayer; }
        set
        {
            if (_currentPlayer != value)
            {
                PlayerDeactivating?.Invoke(_currentPlayer);
                _currentPlayer = value;
                PlayerActivating?.Invoke(_currentPlayer);
            }
        }
    }

    public CinemachineCamera GetCurrentPlayerCamera()
    {
        if (_currentPlayer == null)
        {
            Debug.LogError("Current player isnt set");
            return null;
        }

        return _currentPlayer.GetComponentInChildren<CinemachineCamera>();
    }
    
    public PlayerStateMachine GetCurrentPlayerController()
    {
        if (_currentPlayer == null)
        {
            Debug.LogError("Current player isnt set");
            return null; 
        }

        return _currentPlayer?.GetComponent<PlayerStateMachine>();
    }
}