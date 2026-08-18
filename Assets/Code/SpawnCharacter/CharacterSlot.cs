using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class CharacterSlot : MonoBehaviour
{
    [SerializeField] InputActionReference SpawnButton;
    ScriptableStats characterStats;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] Slider cooldownImage;
    bool cooldownFinished = false;
    float nextSpawnableTime;
    float CooldownRemaining;
    [SerializeField] bool showDebugLogs = false;
    void Awake()
    {
        
    }

    void Start()
    {
        SubscribeToInput();
    }

    void SubscribeToInput()
    {
         if (GlobalInputManager.Instance == null)
        {
            Debug.LogError("GlobalInputManager not found! Spawning will not work.");
            return;
        }
        SpawnButton.action.performed += SpawnCharacter;
    }
    void OnDestroy()
    {
        UnsubscribeFromInput();
    }
    void UnsubscribeFromInput()
    {
        if (GlobalInputManager.Instance == null)
        {
            Debug.LogError("GlobalInputManager not found! Spawning will not work.");
            return;
        }
        SpawnButton.action.performed -= SpawnCharacter;
    }
    public void EquipCPU(ScriptableStats stats)
    {
        if (stats == null) return;
        characterStats = stats;
    }

    public void UnequipCPU()
    {
        characterStats = null;
    }
    void SpawnCharacter(InputAction.CallbackContext context)
    {
        if (playerPrefab != null) 
        {
            SpawnPlayer();
            return;
        }
        if (characterStats == null) return;
        if (!cooldownFinished)
        {
            return;
        }
        if (SpawnObjects.PlayerInstance == null)
        {
            if (showDebugLogs)Debug.LogError("No spawner assigned on player spawner!");
            return;
        }

        GameObject spawned = SpawnObjects.PlayerInstance.SpawnFromPlayer(characterStats);

        if (spawned != null)
        {
            nextSpawnableTime = Time.time + characterStats._spawnCooldown;
            cooldownFinished = false;
            FModAudioManager.instance.PlaySoundByName("spawnTroop");
            //FModAudioManager.instance.PlaySoundByName(characterStats.SpawnSoundName);
            if (showDebugLogs)Debug.Log($"Player spawned: {characterStats.name}");
        }
        if (playerPrefab != null) SpawnPlayer();
    }

    void SpawnPlayer()
    {
        GameObject spawned = SpawnObjects.PlayerInstance.SpawnPlayer(playerPrefab);

        if (spawned != null)
        {
            FModAudioManager.instance.PlaySoundByName("spawnTroop");
            if (showDebugLogs)Debug.Log("Player character spawned");
        }
    }

    void Update()
    {
        if (Time.time >= nextSpawnableTime) 
        {
            
            CooldownRemaining = 0f;
            cooldownFinished = true;
            cooldownImage.value = 0;
        }
        else 
        {
            CooldownRemaining = nextSpawnableTime - Time.time;
            UpdateCooldownImage();
        }
    }

    void UpdateCooldownImage()
    {
        if (characterStats == null) return;
        cooldownImage.value = CooldownRemaining/characterStats._spawnCooldown;
    }
}
