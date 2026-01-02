using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class PlayerSwitch : MonoBehaviour
{
    [SerializeField] List<GameObject> players = new List<GameObject>();
    private List<CinemachineCamera> playerCameras = new List<CinemachineCamera>();

    int activePlayerIndex = 0;
    int PlayerIDNumber;
    
    void Start()
    {
        PlayerIDNumber = 1;

        // Subscribe to input from the GlobalInputManager
        var controlManager = GlobalInputManager.Instance.InputActions.ControlManager;
        
        controlManager.NextPlayer.performed += SwitchPlayerRight;
        controlManager.PreviousPlayer.performed += SwitchPlayerLeft;

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
            
            // Initialize all players as inactive
            var playerSM = player.GetComponent<PlayerStateMachine>();
            if (playerSM != null)
            {
                playerSM.SetActive(false);
            }
        }

        // Set up ActivePlayer reference and camera priorities
        if (players.Count > 0)
        {
            ActivePlayer.Instance.CurrentPlayer = players[activePlayerIndex];
            playerCameras[activePlayerIndex].Priority = 2;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe when destroyed
        if (GlobalInputManager.Instance != null)
        {
            var controlManager = GlobalInputManager.Instance.InputActions.ControlManager;
            controlManager.NextPlayer.performed -= SwitchPlayerRight;
            controlManager.PreviousPlayer.performed -= SwitchPlayerLeft;
        }
    }

    public void SwitchPlayerRight(InputAction.CallbackContext context)
    {
        if (!context.performed || players.Count == 0)
            return;

        RemoveNullPlayers();

        if (players.Count == 0 || playerCameras.Count == 0)
        {
            Debug.Log("No players available to switch to.");
            return;
        }

        if (activePlayerIndex >= 0 && activePlayerIndex < players.Count && players[activePlayerIndex] != null)
        {
            ActivePlayer.Instance.CurrentPlayer.GetComponent<PlayerStateMachine>().PlayerCommander.ClearAllCommands();
            playerCameras[activePlayerIndex].Priority = 0;
        }

        int startIndex = activePlayerIndex;
        do
        {
            activePlayerIndex = (activePlayerIndex + 1) % players.Count;
        }
        while (players[activePlayerIndex] == null && activePlayerIndex != startIndex);

        ActivatePlayer(activePlayerIndex);
    }

    public void SwitchPlayerLeft(InputAction.CallbackContext context)
    {
        if (!context.performed || players.Count == 0)
            return;
        
        RemoveNullPlayers();

        if (players.Count == 0 || playerCameras.Count == 0)
        {
            Debug.Log("No players available to switch to.");
            return;
        }

        if (activePlayerIndex >= 0 && activePlayerIndex < players.Count && players[activePlayerIndex] != null)
        {
            ActivePlayer.Instance.CurrentPlayer.GetComponent<PlayerStateMachine>().PlayerCommander.ClearAllCommands();
            playerCameras[activePlayerIndex].Priority = 0;
        }

        int startIndex = activePlayerIndex;
        do
        {
            activePlayerIndex = (activePlayerIndex - 1 + players.Count) % players.Count;
        }
        while (players[activePlayerIndex] == null && activePlayerIndex != startIndex);

        ActivatePlayer(activePlayerIndex);
    }

    private void RemoveNullPlayers()
    {
        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i] == null)
            {
                players.RemoveAt(i);
                playerCameras.RemoveAt(i);
                if (i < activePlayerIndex)
                {
                    activePlayerIndex--;
                }
            }
        }
        activePlayerIndex = Mathf.Clamp(activePlayerIndex, 0, players.Count - 1);
    }

    void ActivatePlayer(int index)
    {
        Debug.Log($"ActivatePlayer called for index {index}");
        
        // Deactivate all players first
        for (int i = 0; i < players.Count; i++)
        {
            var playerSM = players[i].GetComponent<PlayerStateMachine>();
            if (playerSM == null) continue;

            if (i == index)
            {
                // Activate this player
                playerCameras[i].Priority = 2;
                ActivePlayer.Instance.CurrentPlayer = players[i];
                playerSM.SetActive(true);
            }
            else
            {
                // Deactivate other players
                playerCameras[i].Priority = 0;
                playerSM.SetActive(false);
            }
        }
    }

    public void AddPlayer(GameObject newPlayer)
    {
        var managePlayer = newPlayer.GetComponent<PlayerStateMachine>();
        managePlayer.PlayerID = PlayerIDNumber++;
        players.Add(newPlayer);

        var newCam = newPlayer.GetComponentInChildren<CinemachineCamera>();
        if (newCam != null)
        {
            newCam.Priority = 0;
            playerCameras.Add(newCam);
        }
        else
        {
            Debug.LogError($"No CinemachineCamera found in {newPlayer.name}");
        }

        Debug.Log($"Added new player. Total players: {players.Count}");
    }

    public void RemovePlayer(GameObject playerToRemove)
    {
        int index = players.IndexOf(playerToRemove);
        if (index == -1)
        {
            Debug.LogWarning("Tried to remove player, but it wasn't in the list.");
            return;
        }

        bool wasActive = index == activePlayerIndex;

        players.RemoveAt(index);
        playerCameras.RemoveAt(index);
        Debug.Log($"Removed player at index {index}. Total players: {players.Count}");

        if (wasActive)
        {
            if (players.Count > 0)
            {
                activePlayerIndex = Mathf.Clamp(index, 0, players.Count - 1);
                ActivatePlayer(activePlayerIndex);
            }
            else
            {
                activePlayerIndex = -1;
                Debug.Log("No players left to control.");
            }
        }
        else if (activePlayerIndex > index)
        {
            activePlayerIndex--;
        }

        // Optional: reassign PlayerIDs
        for (int i = 0; i < players.Count; i++)
        {
            players[i].GetComponent<PlayerStateMachine>().PlayerID = i;
        }
    }
}