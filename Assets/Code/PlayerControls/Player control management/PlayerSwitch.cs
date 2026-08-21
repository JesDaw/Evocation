using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
/// <summary>
/// This script tracks what player characters an in pay, lets the player switch between controlling them and 
/// also maintains the active player script
/// </summary>
public class PlayerSwitch : MonoBehaviour
{
    [SerializeField] List<GameObject> players = new List<GameObject>();
    public List<GameObject> Players => players;
    [SerializeField] GameObject cameraBounds;
    [SerializeField] bool AutoStartAsPlayerControls = false;
    [SerializeField] bool DebugLogs = false;
    List<CinemachineCamera> playerCameras = new List<CinemachineCamera>();

    int activePlayerIndex = 0;
    int PlayerIDNumber;
    public static PlayerSwitch Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    void SetupCameraConfiner(CinemachineCamera cam)
    {
        if (cam == null) return;

        var confiner = cam.GetComponent<CinemachineConfiner3D>();
        if (confiner == null)
        {
            confiner = cam.gameObject.AddComponent<CinemachineConfiner3D>();
        }

        if (cameraBounds != null)
        {
            Collider boundsCollider = cameraBounds.GetComponent<Collider>();
            if (boundsCollider != null)
            {
                confiner.BoundingVolume = boundsCollider;
            }
            else
            {
                Debug.LogError("CameraBounds object has no Collider component!");
            }
        }
        else
        {
            Debug.LogError("CameraBounds reference not set in PlayerSwitch!");
        }
    }

    void Start()
    {
        if (GlobalInputManager.Instance == null) Debug.LogWarning("playerCameras switch cant find GlobalInputManager.Instance");

        PlayerIDNumber = 1;
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
            
            var playerSM = player.GetComponent<PlayerStateMachine>();
            if (playerSM != null)
            {
                playerSM.SetActive(false);
            }
        }
        if (DebugLogs) Debug.Log($"players.Count = {players.Count}");
        if (players.Count > 0)
        {
            ActivePlayer.Instance.CurrentPlayer = players[activePlayerIndex];
            playerCameras[activePlayerIndex].Priority = 2;
            ActivePlayer.Instance.CurrentPlayer.SetActive(true);
            if (AutoStartAsPlayerControls) 
            {
                CameraControlSwitcher.Instance.SwitchToPlayerControl();
            }

        }

        for (int i = 0; i < playerCameras.Count; i++)
        {
            SetupCameraConfiner(playerCameras[i]);
        }

    }

    void OnDestroy()
    {
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
        SwitchPlayerRight();
    }

    void SwitchPlayerRight()
    {
       

        RemoveNullPlayers();

        if (players.Count == 0 || playerCameras.Count == 0)
        {
            Debug.Log("No players available to switch to.");
            return;
        }

        if (activePlayerIndex >= 0 && activePlayerIndex < players.Count && players[activePlayerIndex] != null)
        {
            var playerSM = players[activePlayerIndex].GetComponent<PlayerStateMachine>();
            if (playerSM != null && playerSM.PlayerCommander != null)
            {
                playerSM.PlayerCommander.ClearAllCommands();
            }
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
            var playerSM = players[activePlayerIndex].GetComponent<PlayerStateMachine>();
            if (playerSM != null && playerSM.PlayerCommander != null)
            {
                playerSM.PlayerCommander.ClearAllCommands();
            }
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

    void RemoveNullPlayers()
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
        if (index < 0 || index >= players.Count)
        {
            Debug.LogError($"Invalid player index: {index}");
            return;
        }

        if (players[index] == null)
        {
            Debug.LogError($"Player at index {index} is null!");
            return;
        }

        bool isFreeCamActive = CameraControlSwitcher.Instance != null && 
                               CameraControlSwitcher.Instance.FreeCamIsActive;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null) continue;

            var playerSM = players[i].GetComponent<PlayerStateMachine>();
            if (playerSM == null) continue;

            if (i == index)
            {
                // Activate this player
                if (!isFreeCamActive)
                {
                    playerCameras[i].Priority = 2;
                }
                else
                {
                    playerCameras[i].Priority = 0;
                }
                
                ActivePlayer.Instance.CurrentPlayer = players[i];
                playerSM.SetActive(!isFreeCamActive);
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
            SetupCameraConfiner(newCam);
            playerCameras.Add(newCam);
        }
        else
        {
            Debug.LogError($"No CinemachineCamera found in {newPlayer.name}");
        }
        if (players.Count == 1) SwitchPlayerRight();

        if (PlayerLivesManager.Instance != null) PlayerLivesManager.Instance.OnPlayerAdded(newPlayer);
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
                ActivePlayer.Instance.CurrentPlayer = null;
                Debug.Log("No players left to control.");
            }
        }
        else if (activePlayerIndex > index)
        {
            activePlayerIndex--;
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
            {
                var playerSM = players[i].GetComponent<PlayerStateMachine>();
                if (playerSM != null)
                {
                    playerSM.PlayerID = i;
                }
            }
        }
    }
}