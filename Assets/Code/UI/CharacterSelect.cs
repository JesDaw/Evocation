using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using ChristinaCreatesGames.UI;
public class CharacterSelect : MonoBehaviour
{

    [Header("Character summary objects")]
    [SerializeField] TMP_Text characterNameText;
    [SerializeField] TMP_Text characterDescriptionText;
    [SerializeField] Image characterImage;
    

    public List<CharacterData> party = new List<CharacterData>();
    public int partySize = 5;
    [Header("Loadlout screen")]
    [SerializeField] PlayerRelationshipSO playerRelationshipSO;
    [SerializeField] CharacterButton[] LoadoutSlotsUI;
    [Header("Party size limitations")]
    [SerializeField] TMP_Text partyCountText;
    [SerializeField] GameObject maxMessage;
    [SerializeField] Image[] partySlots;
    [SerializeField] GameObject[] partySlotsGameplayUI;
    [SerializeField] TMP_Text[] characterPrice;
    [Header("Gameplay UI Party Bar")]
    [SerializeField] CharacterSlot[] characterSlots;

    CharacterData lastClicked = null;

    public static CharacterSelect Instance { get; private set; }
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
        maxMessage.SetActive(false);
        UpdateSelactableUI();
        UpdatePartyUI();
    }

    void UpdateSelactableUI()
    {
        var relationshipStats = playerRelationshipSO.RelationStats;
        
        int currentClanIndex = 0;
        int currentMemberIndex = 0;

        foreach (CharacterButton frame in LoadoutSlotsUI)
        {
            bool frameAssigned = false;
            for (; currentClanIndex < relationshipStats.Length; currentClanIndex++)
            {
                var clan = relationshipStats[currentClanIndex];
                var members = clan.clanStats.all_stats_scripts;

                for (; currentMemberIndex < members.Length; currentMemberIndex++)
                {
                    var member = members[currentMemberIndex]; 

                    if (member.RelationshipLevelRequironment <= clan.Depth_Level)
                    {
                        frame.character = member;
                        frame.UpdateFrame();
                        currentMemberIndex++; 
                        frameAssigned = true;
                        break;
                    }
                }

                if (frameAssigned) break;
                currentMemberIndex = 0;
            }
            if (!frameAssigned) break;
        }
    }

    public void OnCharacterClicked(CharacterData character)
    {
        //first click shows the character info only
        if (lastClicked != character)
        {
            ShowCharacterInfo(character);
            return;
        }

        //second click toggle the character in/out of party
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
        lastClicked = character;
        //Debug.Log($"last clicked = {character.characterName}");
        //FModAudioManager.instance.PlaySoundByName("showCharacterInfo");
        characterImage.enabled = true;
        characterImage.sprite = character.portrait;
        characterNameText.text = character.characterName;
        characterDescriptionText.text = character.description;
    }

    public void AddCharacterToParty(CharacterData character)
    {
        if (party.Count < partySize)
            {
                FModAudioManager.instance.PlaySoundByName("addCharacterToParty");
                if (character.SoundName != "") FModAudioManager.instance.PlaySoundByName(character.SoundName);

                party.Add(character);
            }
            else
            {
                StartCoroutine(MaxMessageRoutine());
                return;
            }
    }

    public void CommitParty()
    {
        for (int i = 0; i < characterSlots.Length - 1; i++)
        {
            if (i > characterSlots.Length)
            {
                Debug.LogWarning($"no character slot at index {i}");
                continue;
            }
            if (i < characterSlots.Length && i > party.Count) characterSlots[i].UnequipCPU();
            if (i < characterSlots.Length && i < party.Count) characterSlots[i].EquipCPU(party[i].scriptableStats);
        }
    }

    IEnumerator MaxMessageRoutine()
    {
        maxMessage.SetActive(true);
        yield return new WaitForSeconds(2f);
        maxMessage.SetActive(false);
    }

    void UpdatePartyUI()
    {
        partyCountText.text = $"{party.Count}/{partySize}";

        for (int i = 0; i < partySlots.Length; i++)
        {
            partySlots[i].enabled = false;
            partySlots[i].sprite = null;
            partySlotsGameplayUI[i].transform.Find("CharacterHeadshot").gameObject.GetComponent<Image>().enabled = false;
            partySlotsGameplayUI[i].transform.Find("CharacterHeadshot").gameObject.GetComponent<Image>().sprite = null;
            characterPrice[i].enabled = false;

        }

        for (int i = 0; i < party.Count; i++)
        {
            partySlots[i].enabled = true;
            partySlots[i].sprite = party[i].headshot;
            partySlotsGameplayUI[i].transform.Find("CharacterHeadshot").gameObject.GetComponent<Image>().enabled = true;
            partySlotsGameplayUI[i].transform.Find("CharacterHeadshot").gameObject.GetComponent<Image>().sprite = party[i].headshot;
            characterPrice[i].enabled = true;
            partySlotsGameplayUI[i].GetComponent<HotkeyButton>()._characterPrice = party[i].scriptableStats._spawnCost;
            characterPrice[i].text = party[i].scriptableStats._spawnCost.ToString();

        }
    }
}
