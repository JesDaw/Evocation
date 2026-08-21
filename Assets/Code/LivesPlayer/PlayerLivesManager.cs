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
                if (player != null) SubscribeToPlayerDeath(player); 
            }
        }
    }

    public void GainLife()
    {
        if (DebugLogs) Debug.Log($"HeGain life");
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
        if (DebugLogs) Debug.Log($"On player added");
        SubscribeToPlayerDeath(player);
    }
    void SubscribeToPlayerDeath(GameObject player)
    {
        if (DebugLogs) Debug.Log($"Subscribe to player death");
        Stats stats = player.GetComponent<Stats>();
        if (stats != null)
        {
            stats.OnDeath.DynamicCalls += () => LooseLife(player);
        }
    }
    public void LooseLife(GameObject deadPlayer)
    {
        if (DebugLogs) Debug.Log($"handling loose life");
        LifeCount--;
        canSpawnMore = true;
        PlayerLivesDisplay.Instance.UpdateTorchDisplay();

        

        if (LifeCount <= 0)
        {
            HandlePlayerDeath(deadPlayer);   
        }
        else
        {
            HandleLastPlayerDeath(deadPlayer);
        }
        
    }

    void HandlePlayerDeath(GameObject deadPlayer)
    {
        CheckActivePlayerDeathCam(deadPlayer, true);
        if (deadPlayer != null) PlayerSwitch.Instance.RemovePlayer(deadPlayer);

    }

    void HandleLastPlayerDeath(GameObject deadPlayer) 
    {
        CheckActivePlayerDeathCam(deadPlayer, false);
        if (deadPlayer != null) PlayerSwitch.Instance.RemovePlayer(deadPlayer);
        OutOfLives.Invoke();
    }

    void CheckActivePlayerDeathCam(GameObject deadPlayer, bool switchControls)
    {
        if (deadPlayer == ActivePlayer.Instance.CurrentPlayer)
        {
            if (CameraControlSwitcher.Instance != null && !CameraControlSwitcher.Instance.FreeCamIsActive)
            {
                    //CameraControlSwitcher.Instance.SwitchToFreeCamAtPosition(deathCameraPosition, deathCameraFOV);
                    CameraControlSwitcher.Instance.SwitchToCameraControl(switchControls);
            }
            else
            {
                // the active player died but the player is in free cam
            }
        }
    }
}