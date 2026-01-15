using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Unity.Cinemachine;

public class PlayerLivesManager : MonoBehaviour
{
    [SerializeField] public IntVeriable LifeCount;
    [SerializeField] public int MaxLives;
    [SerializeField] UnityEvent _loose_game;
    [SerializeField] ActivePlayer activePlayer;
    [SerializeField] PlayerSwitch playerSwitch;
    [SerializeField] PlayerLivesDisplay playerLivesDisplay;
    [SerializeField] CameraControlSwitcher cameraControlSwitcher;
    public bool canSpawnMore = true;
    public static PlayerLivesManager Instance { get; private set; }

    private void SubscribeToPlayerDeath(GameObject player)
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Subscribe for existing players
        if (playerSwitch != null)
        {
            foreach (GameObject player in playerSwitch.Players)
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
            LifeCount._Value++;
            playerLivesDisplay.UpdateTorchDisplay();
            if (LifeCount._Value >= MaxLives) 
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
        LifeCount._Value--;
        canSpawnMore = true;
        playerLivesDisplay.UpdateTorchDisplay();

        if (LifeCount._Value <= 0)
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
        var currentPlayer = activePlayer.GetCurrentPlayerController();
        
        if (currentPlayer != null)
        {
            LooseLife(currentPlayer.gameObject);
        }
        else
        {
            LifeCount._Value--;
            canSpawnMore = true;
            playerLivesDisplay.UpdateTorchDisplay();

            if (LifeCount._Value <= 0)
            {
                _loose_game.Invoke();
            }
        }
    }

    private void HandlePlayerDeath(GameObject deadPlayer)
    {
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
            playerSwitch.RemovePlayer(deadPlayer);
        }

        if (cameraControlSwitcher != null)
        {
            cameraControlSwitcher.SwitchToFreeCamAtPosition(deathCameraPosition, deathCameraFOV);
        }
        else
        {
            Debug.LogError("CameraControlSwitcher not assigned to PlayerLivesManager!");
        }
    }

    private void HandleGameOver(GameObject deadPlayer)
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

        if (cameraControlSwitcher != null)
        {
            cameraControlSwitcher.SwitchToFreeCamAtPosition(deathCameraPosition, deathCameraFOV);
        }
    }
}