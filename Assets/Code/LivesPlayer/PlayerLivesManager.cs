using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Unity.Cinemachine;
using System.Data.SqlTypes;

public class PlayerLivesManager : MonoBehaviour
{
    [SerializeField] public int MaxLives;
    [SerializeField] UnityEvent OutOfLives;
    public bool canSpawnMore = true;
    public int LifeCount = 1;
    public static PlayerLivesManager Instance { get; private set; }
    [SerializeField] bool DebugLogs;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        if (DebugLogs) Debug.Log($"Here");
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

    public void OnPlayerAdded(GameObject player)
    {
        if (DebugLogs) Debug.Log($"Here");
        SubscribeToPlayerDeath(player);
    }
    void SubscribeToPlayerDeath(GameObject player)
    {
        if (DebugLogs) Debug.Log($"Here");
        Stats stats = player.GetComponent<Stats>();
        if (stats != null)
        {
            stats.OnDeath.DynamicCalls += () => LooseLife(player);
        }
    }

    

    public void LooseLife()
    {
        if (DebugLogs) Debug.Log($"Here");
        var currentPlayer = ActivePlayer.Instance.GetCurrentPlayerController();
        
        if (currentPlayer != null)
        {
            LooseLife(currentPlayer.gameObject);
        }
        else // this gets called if the current player is null idk if this path exicutes or the other one but its probably always the same when the player is out of lives
        {
            LifeCount--;
            canSpawnMore = true;
            PlayerLivesDisplay.Instance.UpdateTorchDisplay();

            if (LifeCount <= 0)
            {
                OutOfLives.Invoke();
            }
        }
    }
    public void LooseLife(GameObject deadPlayer)
    {
        if (DebugLogs) Debug.Log($"Here");
        LifeCount--;
        canSpawnMore = true;
        PlayerLivesDisplay.Instance.UpdateTorchDisplay();

        if (LifeCount <= 0)
        {
            HandleOutOfLives(deadPlayer);
        }
        else
        {
            HandlePlayerDeath(deadPlayer);
        }
    }

    void HandlePlayerDeath(GameObject deadPlayer)
    {
        if (DebugLogs) Debug.Log($"Here");
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

    void HandleOutOfLives(GameObject deadPlayer) 
    {
        if (DebugLogs) Debug.Log($"Here");
        CinemachineCamera deadPlayerCam = deadPlayer?.GetComponentInChildren<CinemachineCamera>();
        Vector3 deathCameraPosition = Vector3.zero;
        float deathCameraFOV = 60f;
        
        if (deadPlayerCam != null)
        {
            deathCameraPosition = deadPlayerCam.transform.position;
            deathCameraFOV = deadPlayerCam.Lens.FieldOfView;
        }

        OutOfLives.Invoke();

        if (CameraControlSwitcher.Instance != null)
        {
            CameraControlSwitcher.Instance.SwitchToFreeCamAtPosition(deathCameraPosition, deathCameraFOV);
        }
    }
}