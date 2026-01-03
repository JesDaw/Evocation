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

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

    /// <summary>
    /// Called when a player dies. Pass the dead player's GameObject to remove it.
    /// </summary>
    /// <param name="deadPlayer">The GameObject of the player who died</param>
    public void LooseLife(GameObject deadPlayer)
    {
        LifeCount._Value--;
        canSpawnMore = true;
        playerLivesDisplay.UpdateTorchDisplay();

        if (LifeCount._Value <= 0)
        {
            // Game over - switch to freecam at dead player's position
            HandleGameOver(deadPlayer);
        }
        else
        {
            // Still have lives - switch to ghost cam, then switch active player
            HandlePlayerDeath(deadPlayer);
        }
    }

    /// <summary>
    /// Legacy method - tries to figure out which player died.
    /// Better to use LooseLife(GameObject deadPlayer) instead.
    /// </summary>
    public void LooseLife()
    {
        // This is called from player's onDeath event
        // The problem is we don't know WHICH player died
        var currentPlayer = activePlayer.GetCurrentPlayerController();
        
        if (currentPlayer != null)
        {
            LooseLife(currentPlayer.gameObject);
        }
        else
        {
            // Fallback if we can't determine the player
            LifeCount._Value--;
            canSpawnMore = true;
            playerLivesDisplay.UpdateTorchDisplay();

            if (LifeCount._Value <= 0)
            {
                _loose_game.Invoke();
            }
        }
    }

    /// <summary>
    /// Handle death when there are still lives remaining
    /// </summary>
    private void HandlePlayerDeath(GameObject deadPlayer)
    {
        // Get the dead player's camera position before doing anything
        CinemachineCamera deadPlayerCam = deadPlayer.GetComponentInChildren<CinemachineCamera>();
        Vector3 deathCameraPosition = Vector3.zero;
        float deathCameraFOV = 60f;
        
        if (deadPlayerCam != null)
        {
            deathCameraPosition = deadPlayerCam.transform.position;
            deathCameraFOV = deadPlayerCam.Lens.FieldOfView;
        }

        // Remove the dead player from the list and switch to next player
        // This will update ActivePlayer.CurrentPlayer to the next available player
        if (deadPlayer != null)
        {
            playerSwitch.RemovePlayer(deadPlayer);
        }

        // Now switch to freecam mode at the death location
        if (cameraControlSwitcher != null)
        {
            cameraControlSwitcher.SwitchToFreeCamAtPosition(deathCameraPosition, deathCameraFOV);
        }
        else
        {
            Debug.LogError("CameraControlSwitcher not assigned to PlayerLivesManager!");
        }
    }

    /// <summary>
    /// Handle game over when no lives remain
    /// </summary>
    private void HandleGameOver(GameObject deadPlayer)
    {
        // Get the dead player's camera position
        CinemachineCamera deadPlayerCam = deadPlayer?.GetComponentInChildren<CinemachineCamera>();
        Vector3 deathCameraPosition = Vector3.zero;
        float deathCameraFOV = 60f;
        
        if (deadPlayerCam != null)
        {
            deathCameraPosition = deadPlayerCam.transform.position;
            deathCameraFOV = deadPlayerCam.Lens.FieldOfView;
        }

        // Invoke game over events
        _loose_game.Invoke();

        // Switch to freecam at death location
        if (cameraControlSwitcher != null)
        {
            cameraControlSwitcher.SwitchToFreeCamAtPosition(deathCameraPosition, deathCameraFOV);
        }
    }
}