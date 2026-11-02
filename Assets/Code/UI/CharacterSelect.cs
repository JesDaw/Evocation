using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CharacterSelect : MonoBehaviour
{
    public static CharacterSelect Instance { get; private set; }

    [Header("Data Source & Settings")]
    [SerializeField] PlayerRelationshipSO playerRelationshipSO;
    [SerializeField] int partySize = 5;
    [SerializeField] bool requireDoubleClickToEquip = false;

    [Header("Parent Containers (Drag the 3 Parents Here!)")]
    [Tooltip("Parent of all selectable roster CharacterButtons")]
    [SerializeField] Transform loadoutGridParent;
    [Tooltip("Drag 'LoadoutFrames' here")]
    [SerializeField] Transform partyPreviewParent;
    [Tooltip("Drag 'CharacterBar' (parent of GameplayCharacetrSlot objects) here")]
    [SerializeField] Transform gameplayPartyBarParent;

    [Header("Character Summary UI")]
    [SerializeField] TMP_Text characterNameText;
    [SerializeField] TMP_Text characterDescriptionText;
    [SerializeField] TMP_Text characterStatsText;
    [SerializeField] Image characterImage;

    [Header("Character Summary Style")]
    [Tooltip("TMPEffects tag for the name, without the angle brackets. Needs a TMPAnimator on the name text.")]
    [SerializeField] string nameEffect = "wave amp=4";

    [Header("Party Status UI")]
    [SerializeField] TMP_Text partyCountText;
    [SerializeField] GameObject maxMessage;

    // ── Dynamically Populated Runtime State ──
    [HideInInspector] public List<CharacterData> party = new List<CharacterData>();
    private CharacterButton[] loadoutSlotsUI;
    private Image[] partyPreviewSlots;
    private CharacterSlot[] gameplayCharacterSlots;
    private CharacterData lastClicked = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CacheHierarchyReferences();
    }

    private void CacheHierarchyReferences()
    {
        // 1. Roster buttons on Loadout screen
        if (loadoutGridParent != null)
            loadoutSlotsUI = loadoutGridParent.GetComponentsInChildren<CharacterButton>(true);

        // 2. LoadoutFrames -> Loadout slot frame -> Loadout slot (leaf Image)
        if (partyPreviewParent != null)
        {
            List<Image> previewImages = new List<Image>();
            foreach (Transform frameTransform in partyPreviewParent)
            {
                if (frameTransform.childCount > 0)
                {
                    Image leafImage = frameTransform.GetChild(0).GetComponent<Image>();
                    if (leafImage != null) previewImages.Add(leafImage);
                }
            }
            partyPreviewSlots = previewImages.ToArray();
        }

        // 3. CharacterBar -> GameplayCharacetrSlot (CharacterSlot scripts in hierarchy order)
        if (gameplayPartyBarParent != null)
        {
            gameplayCharacterSlots = gameplayPartyBarParent.GetComponentsInChildren<CharacterSlot>(true);
        }
    }

    void Start()
    {
        if (maxMessage != null) maxMessage.SetActive(false);
        UpdateSelectableUI();
        UpdatePartyUI();
        ShowDefaultCharacter();
    }

    // Fills the summary with the first unlocked character until the player picks one.
    void ShowDefaultCharacter()
    {
        if (loadoutSlotsUI == null) return;

        foreach (CharacterButton slot in loadoutSlotsUI)
        {
            if (slot.Character != null)
            {
                ShowCharacterInfo(slot.Character);
                return;
            }
        }
    }

    void UpdateSelectableUI()
    {
        if (playerRelationshipSO == null || loadoutSlotsUI == null) return;

        List<CharacterData> unlockedCharacters = new List<CharacterData>();
        foreach (var clan in playerRelationshipSO.RelationStats)
        {
            if (clan?.clanStats?.all_stats_scripts == null) continue;

            foreach (var member in clan.clanStats.all_stats_scripts)
            {
                if (member != null && member.RelationshipLevelRequironment <= clan.Depth_Level)
                {
                    unlockedCharacters.Add(member);
                }
            }
        }

        for (int i = 0; i < loadoutSlotsUI.Length; i++)
        {
            CharacterData assignedChar = (i < unlockedCharacters.Count) ? unlockedCharacters[i] : null;
            loadoutSlotsUI[i].AssignCharacter(assignedChar);
        }
    }

    public void OnCharacterClicked(CharacterData character)
    {
        if (character == null) return;

        ShowCharacterInfo(character);

        if (requireDoubleClickToEquip && lastClicked != character)
        {
            lastClicked = character;
            return;
        }

        lastClicked = null;

        if (party.Contains(character))
        {
            FModAudioManager.instance.PlaySoundByName("removeCharacterFromParty");
            party.Remove(character);
        }
        else
        {
            AddCharacterToParty(character);
        }

        UpdatePartyUI();
    }

    public void UpdateCurrentDesplayedCharacter(CharacterData character)
    {
        ShowCharacterInfo(character);
    }

    public void ShowCharacterInfo(CharacterData character)
    {
        if (character == null) return;

        if (characterImage != null)
        {
            characterImage.enabled = true;
            characterImage.sprite = character.portrait;
        }
        if (characterNameText != null) characterNameText.text = WithEffect(character.characterName, nameEffect);

        // The first line says what the character does, and the lines after it are its stats.
        int lineEnd = character.description.IndexOf('\n');
        string firstLine = lineEnd < 0 ? character.description : character.description.Substring(0, lineEnd);
        string stats = lineEnd < 0 ? "" : character.description.Substring(lineEnd + 1);
        if (character.scriptableStats != null) stats += $"\nCost: {character.scriptableStats._spawnCost}";

        if (characterDescriptionText != null) characterDescriptionText.text = firstLine;
        if (characterStatsText != null) characterStatsText.text = stats;
    }

    static string WithEffect(string text, string effect)
    {
        if (string.IsNullOrEmpty(effect)) return text;
        return $"<{effect}>{text}</{effect.Split(' ')[0]}>";
    }

    public void AddCharacterToParty(CharacterData character)
    {
        if (party.Count < partySize)
        {
            FModAudioManager.instance.PlaySoundByName("addCharacterToParty");
            if (!string.IsNullOrEmpty(character.SoundName))
                FModAudioManager.instance.PlaySoundByName(character.SoundName);

            party.Add(character);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(MaxMessageRoutine());
        }
    }

    void UpdatePartyUI()
    {
        if (partyCountText != null)
            partyCountText.text = $"{party.Count}/{partySize}";

        // 1. Update Loadout Screen Party Preview (LoadoutFrames leaf images)
        if (partyPreviewSlots != null)
        {
            for (int i = 0; i < partyPreviewSlots.Length; i++)
            {
                bool hasMember = i < party.Count;
                partyPreviewSlots[i].enabled = hasMember;
                partyPreviewSlots[i].sprite = hasMember ? party[i].headshot : null;
            }
        }

        // 2. Equip GameplayCharacetrSlots AND update their Headshot + Price UI!
        CommitParty();
    }

    public void CommitParty()
    {
        // Safety fallback if CommitParty is called before Awake or if gameplayPartyBarParent was left blank
        if (gameplayCharacterSlots == null || gameplayCharacterSlots.Length == 0)
        {
            if (gameplayPartyBarParent != null)
                gameplayCharacterSlots = gameplayPartyBarParent.GetComponentsInChildren<CharacterSlot>(true);
            else
            {
                Debug.LogError("[CharacterSelect] 'Gameplay Party Bar Parent' (CharacterBar) is NOT assigned in the Inspector on CharacterSelect!", this);
                return;
            }
        }

        int partyIndex = 0;
        for (int i = 0; i < gameplayCharacterSlots.Length; i++)
        {
            if (gameplayCharacterSlots[i] == null) continue;

            // Skip the Player's slot if one of the slots has playerPrefab assigned
            if (gameplayCharacterSlots[i].IsPlayerSlot) continue;

            if (partyIndex < party.Count)
            {
                gameplayCharacterSlots[i].EquipCPU(party[partyIndex]);
                partyIndex++;
            }
            else
            {
                gameplayCharacterSlots[i].UnequipCPU();
            }
        }
    }

    IEnumerator MaxMessageRoutine()
    {
        if (maxMessage != null) maxMessage.SetActive(true);
        yield return new WaitForSeconds(2f);
        if (maxMessage != null) maxMessage.SetActive(false);
    }
}