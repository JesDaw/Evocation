using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using ChristinaCreatesGames.UI;

public class CharacterSlot : MonoBehaviour
{
    [Header("Input & Cooldown")]
    [SerializeField] int slotNumber;
    [SerializeField] Slider cooldownImage;

    [Header("Player Only Stuff (Leave blank for CPU slots!)")]
    [SerializeField] GameObject playerPrefab;
    [SerializeField] ScriptableStats PlayerStats;

    [Header("Debug")]
    [SerializeField] bool showDebugLogs = true;

    // Auto-cached children on this GameplayCharacetrSlot
    private Image headshotImage;
    private TMP_Text priceText;
    private HotkeyButton hotkeyButton;
    private bool isInitialized = false;

    private ScriptableStats characterStats;
    private bool cooldownFinished = false;
    private float nextSpawnableTime;
    private float cooldownRemaining;

    // True ONLY if this slot is the Player's own slot (has playerPrefab assigned)
    public bool IsPlayerSlot => playerPrefab != null;


    void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (isInitialized) return;
        isInitialized = true;

        hotkeyButton = GetComponent<HotkeyButton>();

        // 1. Find "CharacterHeadshot" child Image
        Transform headshotTrans = transform.Find("CharacterHeadshot");
        if (headshotTrans != null)
        {
            headshotImage = headshotTrans.GetComponent<Image>();
            // Also auto-assign HotkeyButton's HeadshotImage so HotkeyButton.Update() never breaks!
            if (hotkeyButton != null && hotkeyButton.HeadshotImage == null)
                hotkeyButton.HeadshotImage = headshotImage;
        }

        // 2. Find "Price" child TMP_Text
        Transform priceTrans = transform.Find("Price");
        if (priceTrans != null)
        {
            priceText = priceTrans.GetComponent<TMP_Text>();
        }

        // 3. Find "CooldownTintSlider" child Slider if not set
        if (cooldownImage == null)
        {
            Transform sliderTrans = transform.Find("CooldownTintSlider");
            if (sliderTrans != null)
                cooldownImage = sliderTrans.GetComponent<Slider>();
        }

        if (IsPlayerSlot && PlayerStats != null)
        {
            characterStats = PlayerStats;
        }
    }

    void Start() => SubscribeToInput();
    void OnDestroy() => UnsubscribeFromInput();

    /// <summary>
    /// Equips the CPU character AND updates CharacterHeadshot, Price, and HotkeyButton all together!
    /// </summary>
    public void EquipCPU(CharacterData data)
    {
        EnsureInitialized();

        if (IsPlayerSlot) return;

        if (data == null || data.scriptableStats == null)
        {
            UnequipCPU();
            return;
        }

        characterStats = data.scriptableStats;

        if (headshotImage != null)
        {
            headshotImage.gameObject.SetActive(true);
            headshotImage.enabled = true;
            headshotImage.sprite = data.headshot;
        }
        else
        {
            Debug.LogError($"[CharacterSlot] Could not find 'CharacterHeadshot' Image on {gameObject.name}!", this);
        }

        if (priceText != null)
        {
            priceText.gameObject.SetActive(true);
            priceText.enabled = true;
            priceText.text = characterStats._spawnCost.ToString();
        }
        else
        {
            Debug.LogError($"[CharacterSlot] Could not find 'Price' TMP_Text on {gameObject.name}!", this);
        }

        if (hotkeyButton != null)
        {
            hotkeyButton._characterPrice = characterStats._spawnCost;
        }
    }

    public void UnequipCPU()
    {
        EnsureInitialized();

        if (IsPlayerSlot) return;

        characterStats = null;

        if (headshotImage != null)
        {
            headshotImage.enabled = false;
            headshotImage.sprite = null;
        }

        if (priceText != null)
        {
            priceText.enabled = false;
        }

        if (hotkeyButton != null)
        {
            hotkeyButton._characterPrice = 0;
        }
    }

    void SubscribeToInput()
    {
        if (GlobalInputManager.Instance == null) return;
        var Input = GlobalInputManager.Instance.InputActions.SpawnerController;
        switch (slotNumber)
        {
            case 1:
                Input.Spawn1.performed += SpawnCharacter;
                break;
            case 2:
                Input.Spawn2.performed += SpawnCharacter;
                break;
            case 3:
                Input.Spawn3.performed += SpawnCharacter;
                break;
            case 4:
                Input.Spawn4.performed += SpawnCharacter;
                break;
            case 5:
                Input.Spawn5.performed += SpawnCharacter;
                break;
            case 6:
                Input.Spawn6.performed += SpawnCharacter;
                break;
            case 7:
                Input.Spawn7.performed += SpawnCharacter;
                break;
            case 8:
                Input.Spawn8.performed += SpawnCharacter;
                break;
            case 9:
                Input.Spawn9.performed += SpawnCharacter;
                break;
            case 10:
                Input.SpawnPlayer.performed += SpawnCharacter;
                break;
            default:
                Debug.LogError("Invalid Character Slot Number");
                break;
        }
    }

    void UnsubscribeFromInput()
    {
        if (GlobalInputManager.Instance == null) return;
        var Input = GlobalInputManager.Instance.InputActions.SpawnerController;
        switch (slotNumber)
        {
            case 1:
                Input.Spawn1.performed -= SpawnCharacter;
                break;
            case 2:
                Input.Spawn2.performed -= SpawnCharacter;
                break;
            case 3:
                Input.Spawn3.performed -= SpawnCharacter;
                break;
            case 4:
                Input.Spawn4.performed -= SpawnCharacter;
                break;
            case 5:
                Input.Spawn5.performed -= SpawnCharacter;
                break;
            case 6:
                Input.Spawn6.performed -= SpawnCharacter;
                break;
            case 7:
                Input.Spawn7.performed -= SpawnCharacter;
                break;
            case 8:
                Input.Spawn8.performed -= SpawnCharacter;
                break;
            case 9:
                Input.Spawn9.performed -= SpawnCharacter;
                break;
            case 10:
                Input.SpawnPlayer.performed -= SpawnCharacter;
                break;
            default:
                Debug.LogError("Invalid Character Slot Number");
                break;
        }
    }

    void SpawnCharacter(InputAction.CallbackContext context)
    {
        Debug.Log($"SpawnCharacter called");
        if (SpawnObjects.PlayerInstance == null) return;
        if (characterStats == null || !cooldownFinished) return;

        GameObject spawned = (playerPrefab != null)
            ? SpawnObjects.PlayerInstance.SpawnPlayer(playerPrefab)
            : SpawnObjects.PlayerInstance.Spawn(characterStats);

        if (spawned != null)
        {
            nextSpawnableTime = Time.time + characterStats._spawnCooldown;
            cooldownFinished = false;
            FModAudioManager.instance.PlaySoundByName("spawnTroop");
            if (showDebugLogs) Debug.Log($"Player spawned: {characterStats.name}");
        }
    }

    void Update()
    {
        if (Time.time >= nextSpawnableTime)
        {
            cooldownRemaining = 0f;
            cooldownFinished = true;
            if (cooldownImage != null) cooldownImage.value = 0;
        }
        else
        {
            cooldownRemaining = nextSpawnableTime - Time.time;
            if (characterStats != null && cooldownImage != null)
                cooldownImage.value = cooldownRemaining / characterStats._spawnCooldown;
        }
    }
}