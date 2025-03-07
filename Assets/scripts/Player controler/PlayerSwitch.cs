using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

public class PlayerSwitch : MonoBehaviour
{
    [SerializeField] Camera FreeCam;
    [SerializeField] private List<PlayersControlerScriptsManager> players = new List<PlayersControlerScriptsManager>();
    [SerializeField] private List<CinemachineCamera> playerCameras = new List<CinemachineCamera>();

    private int activePlayerIndex = 0;

    private void Start()
    {
        if (players.Count == 0 || playerCameras.Count == 0)
        {
            Debug.LogError("PlayerSwitch: No players or cameras assigned!");
            return;
        }

        // Ensure only the first player is active at the start
        ActivatePlayer(activePlayerIndex);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            SwitchPlayer();
        }
    }

    /// <summary>
    /// Switches control to the next player in the list.
    /// </summary>
    private void SwitchPlayer()
    {
        if (players.Count == 0 || playerCameras.Count == 0) return;

        // Disable current player
        players[activePlayerIndex].DisableControls();
        playerCameras[activePlayerIndex].Priority = 0;

        // Move to the next player
        activePlayerIndex = (activePlayerIndex + 1) % players.Count;

        // Activate the new player
        ActivatePlayer(activePlayerIndex);
    }

    /// <summary>
    /// Activates the player at the given index and disables all others.
    /// </summary>
    private void ActivatePlayer(int index)
    {
        for (int i = 0; i < players.Count; i++)
        {
            bool isActive = (i == index);
            if (isActive){
                players[i].EnagbleControls();
                playerCameras[i].Priority = 1;
            }
            else{
                players[i].DisableControls();
                playerCameras[i].Priority = 0;
            }

        }

        Debug.Log($"Switched to Player {index + 1}");
    }

        /// <summary>
    /// Returns the currently active player controller.
    /// </summary>
    public PlayersControlerScriptsManager GetCurrentPlayerController()
    {
        return (players.Count > 0) ? players[activePlayerIndex] : null;
    }

    /// <summary>
    /// Returns the currently active player camera.
    /// </summary>
    public CinemachineCamera GetCurrentPlayerCamera()
    {
        return (playerCameras.Count > 0) ? playerCameras[activePlayerIndex] : null;
    }
}
