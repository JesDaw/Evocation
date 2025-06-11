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

    private GameObject _currentPlayer;

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
}

