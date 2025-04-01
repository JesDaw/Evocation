using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class PlayerSwitch : MonoBehaviour
{
    [SerializeField] private List<GameObject> players = new List<GameObject>();
    private List<CinemachineCamera> playerCameras = new List<CinemachineCamera>();

    private int activePlayerIndex = 0;
    int PlayerIDNumber;

    void Start()
    {
        PlayerIDNumber = 1;
        if (players.Count == 0)
        {
            Debug.LogError("PlayerSwitch: No players assigned!");
            return;
        }

        InputAction nextPlayerAction = InputSystem.actions.FindAction("NextPlayer");
        if (nextPlayerAction != null)
        {
            nextPlayerAction.performed += SwitchPlayer;
            nextPlayerAction.Enable();
        }
        else
        {
            Debug.LogError("NextPlayer action not found in InputSystem!");
        }

        // Auto-populate player cameras
        foreach (GameObject player in players)
        {
            CinemachineCamera cam = player.GetComponentInChildren<CinemachineCamera>();
            if (cam != null)
            {
                playerCameras.Add(cam);
            }
            else
            {
                Debug.LogError($"No CinemachineCamera found in {player.name}");
            }
        }

        ActivatePlayer(activePlayerIndex);
    }

    public void SwitchPlayer(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (players.Count == 0 || playerCameras.Count == 0) return;

        // Disable current player
        players[activePlayerIndex].GetComponent<PlayersControlerScriptsManager>().DisableControls();
        playerCameras[activePlayerIndex].Priority = 0;

        // Move to the next player
        activePlayerIndex = (activePlayerIndex + 1) % players.Count;

        // Activate the new player
        ActivatePlayer(activePlayerIndex);
    }

    private void ActivatePlayer(int index)
    {
        for (int i = 0; i < players.Count; i++)
        {
            bool isActive = (i == index);
            var playerScript = players[i].GetComponent<PlayersControlerScriptsManager>();

            if (isActive)
            {
                playerScript.EnableControls();
                playerCameras[i].Priority = 2;
            }
            else
            {
                playerScript.DisableControls();
                playerCameras[i].Priority = 0;
            }
        }

        Debug.Log($"Switched to Player {index + 1}");
    }

    public void AddPlayer(GameObject newPlayer)
    {
        Debug.Log("adding" + newPlayer.name);
        PlayersControlerScriptsManager managePlayer = newPlayer.GetComponent<PlayersControlerScriptsManager>();
        managePlayer.DisableControls();
        managePlayer._PlayerID = PlayerIDNumber;
        players.Add(newPlayer);
        CinemachineCamera newCam = newPlayer.GetComponentInChildren<CinemachineCamera>();
        newCam.Priority = 0;
        if (newCam != null)
        {
            playerCameras.Add(newCam);
        }
        else
        {
            Debug.LogError($"No CinemachineCamera found in {newPlayer.name}");
        }

        Debug.Log($"Added new player. Total players: {players.Count}");
    }

    public void RemovePlayer(int ID)
    {
        if (ID < 0 || ID >= players.Count)
        {
            Debug.LogError("PlayerSwitch: Invalid index for removal!");
            return;
        }

        bool wasActive = ID == activePlayerIndex;

        // Remove player and camera at the specified index
        players.RemoveAt(ID);
        playerCameras.RemoveAt(ID);
        Debug.Log($"Removed player at index {ID}. Total players: {players.Count}");

        // If the removed player was the active one, we need to switch to the next player
        if (wasActive)
        {
            if (players.Count > 0)
            {
                // If there are still players left, select the next player
                activePlayerIndex = Mathf.Min(ID, players.Count - 1);  // Adjust the active index to avoid out-of-bounds
                ActivatePlayer(activePlayerIndex);
            }
            else
            {
                // If no players are left, reset the active player index
                activePlayerIndex = -1;
                Debug.Log("No players left to control.");
            }
        }
        else
        {
            // If the removed player wasn't the active one, simply update the player IDs
            for (int i = 0; i < players.Count; i++)
            {
                players[i].GetComponent<PlayersControlerScriptsManager>()._PlayerID = i;
            }
        }
    }





    public PlayersControlerScriptsManager GetCurrentPlayerController()
    {
        return (players.Count > 0) ? players[activePlayerIndex].GetComponent<PlayersControlerScriptsManager>() : null;
    }

    public CinemachineCamera GetCurrentPlayerCamera()
    {
        return (playerCameras.Count > 0) ? playerCameras[activePlayerIndex] : null;
    }
}
