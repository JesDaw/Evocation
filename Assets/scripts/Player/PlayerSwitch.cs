using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class PlayerSwitch : MonoBehaviour
{
    [SerializeField] ActivePlayer activePlayer;
    [SerializeField] private List<GameObject> players = new List<GameObject>();
    private List<CinemachineCamera> playerCameras = new List<CinemachineCamera>();

    private int activePlayerIndex = 0;
    private int PlayerIDNumber;

    void Start()
    {
        PlayerIDNumber = 1;

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

        if (players.Count > 0)
            ActivatePlayer(activePlayerIndex);
    }

    public void SwitchPlayer(InputAction.CallbackContext context)
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
            var currentPlayer = players[activePlayerIndex].GetComponent<PlayersControlerScriptsManager>();
            currentPlayer?.DisableControls();
            playerCameras[activePlayerIndex].Priority = 0;
        }

        int startIndex = activePlayerIndex;
        do
        {
            activePlayerIndex = (activePlayerIndex + 1) % players.Count;
        }
        while (players[activePlayerIndex] == null && activePlayerIndex != startIndex); // Loop to find an active player

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



    private void ActivatePlayer(int index)
    {
        for (int i = 0; i < players.Count; i++)
        {
            var playerScript = players[i].GetComponent<PlayersControlerScriptsManager>();
            if (playerScript == null) continue;

            if (i == index)
            {
                playerScript.EnableControls();
                playerCameras[i].Priority = 2;
                activePlayer.CurrentPlayer = players[i];
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
        Debug.Log("Adding " + newPlayer.name);

        var managePlayer = newPlayer.GetComponent<PlayersControlerScriptsManager>();
        managePlayer.DisableControls();
        managePlayer._PlayerID = PlayerIDNumber++;
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
            activePlayerIndex--; // Adjust index after removal
        }

        // Optional: reassign PlayerIDs
        for (int i = 0; i < players.Count; i++)
        {
            players[i].GetComponent<PlayersControlerScriptsManager>()._PlayerID = i;
        }
    }

    public PlayersControlerScriptsManager GetCurrentPlayerController()
    {
        if (players.Count == 0 || activePlayerIndex < 0 || activePlayerIndex >= players.Count)
            return null;

        return players[activePlayerIndex]?.GetComponent<PlayersControlerScriptsManager>();
    }

    public CinemachineCamera GetCurrentPlayerCamera()
    {
        if (playerCameras.Count == 0 || activePlayerIndex < 0 || activePlayerIndex >= playerCameras.Count)
            return null;

        return playerCameras[activePlayerIndex];
    }
}
