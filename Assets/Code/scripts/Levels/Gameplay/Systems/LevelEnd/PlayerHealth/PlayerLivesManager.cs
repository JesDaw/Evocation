using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Unity.Cinemachine;

public class PlayerLivesManager : MonoBehaviour
{
    [SerializeField] public int MaxLives;
    [SerializeField] UnityEvent _loose_game;
    public bool canSpawnMore = true;
    public int LifeCount = 1;
    public static PlayerLivesManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void SubscribeToPlayerDeath(GameObject player)
    {
        Stats stats = player.GetComponent<Stats>();
        if (stats != null)
        {
            stats.OnDeath.DynamicCalls += () => LooseLife(player);
        }
    }

    public void OnPlayerAdded(GameObject player)
    {
        SubscribeToPlayerDeath(player);
    }

    void Start()
    {
        if (PlayerSwitch.Instance != null)
        {
            foreach (GameObject player in PlayerSwitch.Instance.Players)
            {
                if (player != null)
                    SubscribeToPlayerDeath(player);
            }
        }
    }

    public void GainLife()
    {
        if (canSpawnMore)
        {
            LifeCount++;
            PlayerLivesDisplay.Instance.UpdateTorchDisplay();
            if (LifeCount >= MaxLives) 
            {
                canSpawnMore = false;
            }
        }
        else
        {
            Debug.LogWarning("Max Players reached");
        }
    }

    public void LooseLife(GameObject deadPlayer)
    {
        LifeCount--;
        canSpawnMore = true;
        PlayerLivesDisplay.Instance.UpdateTorchDisplay();

        if (LifeCount <= 0)
        {
            HandleGameOver(deadPlayer);
        }
        else
        {
            HandlePlayerDeath(deadPlayer);
        }
    }

    public void LooseLife()
    {
        var currentPlayer = ActivePlayer.Instance.GetCurrentPlayerController();
        
        if (currentPlayer != null)
        {
            LooseLife(currentPlayer.gameObject);
        }
        else
        {
            LifeCount--;
            canSpawnMore = true;
            PlayerLivesDisplay.Instance.UpdateTorchDisplay();

            if (LifeCount <= 0)
            {
                _loose_game.Invoke();
            }
        }
    }

    void HandlePlayerDeath(GameObject deadPlayer)
    {
        bool isActivePlayer = deadPlayer == ActivePlayer.Instance.CurrentPlayer;
        bool isInFreeCam = CameraControlSwitcher.Instance != null && 
                          CameraControlSwitcher.Instance.FreeCamIsActive;

        CinemachineCamera deadPlayerCam = deadPlayer.GetComponentInChildren<CinemachineCamera>();
        Vector3 deathCameraPosition = Vector3.zero;
        float deathCameraFOV = 60f;
        
        if (deadPlayerCam != null)
        {
            deathCameraPosition = deadPlayerCam.transform.position;
            deathCameraFOV = deadPlayerCam.Lens.FieldOfView;
        }

        if (deadPlayer != null)
        {
            PlayerSwitch.Instance.RemovePlayer(deadPlayer);
        }

        if (isActivePlayer)
        {
            if (CameraControlSwitcher.Instance != null)
            {
                if (!isInFreeCam)
                {
                    CameraControlSwitcher.Instance.SwitchToFreeCamAtPosition(deathCameraPosition, deathCameraFOV);
                }
            }
            else
            {
                Debug.LogError("CameraControlSwitcher not assigned!");
            }
        }
    }

    void HandleGameOver(GameObject deadPlayer)
    {
        CinemachineCamera deadPlayerCam = deadPlayer?.GetComponentInChildren<CinemachineCamera>();
        Vector3 deathCameraPosition = Vector3.zero;
        float deathCameraFOV = 60f;
        
        if (deadPlayerCam != null)
        {
            deathCameraPosition = deadPlayerCam.transform.position;
            deathCameraFOV = deadPlayerCam.Lens.FieldOfView;
        }

        _loose_game.Invoke();

        if (CameraControlSwitcher.Instance != null)
        {
            CameraControlSwitcher.Instance.SwitchToFreeCamAtPosition(deathCameraPosition, deathCameraFOV);
        }
    }
}