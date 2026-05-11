using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;


public class CharacterSelect : MonoBehaviour
{

    [Header("Character summary objects")]
    [SerializeField] TMP_Text characterNameText;
    [SerializeField] TMP_Text characterDescriptionText;
    [SerializeField] Image characterImage;
    

    public List<CharacterData> party = new List<CharacterData>();
    public int partySize = 5;

    [Header("Party size limitations")]
    [SerializeField] TMP_Text partyCountText;
    [SerializeField] GameObject maxMessage;
    [SerializeField] Image[] partySlots;
    [SerializeField] Image[] partySlotsGameplayUI;
    [SerializeField] TMP_Text[] characterPrice;

    CharacterData lastClicked = null;

    void Start()
    {
        maxMessage.SetActive(false);
        UpdatePartyUI();
        if (SpawnController.Instance == null) Debug.LogError($"spawnController not found in {gameObject.name}");
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
            SpawnController.Instance.UnequipCPU(character.scriptableStats);
        }
        else
        {
            AddCharacterToParty(character);
        }

        UpdatePartyUI();
    }

    public void ShowCharacterInfo(CharacterData character)
    {
        if (character == null) return;
        lastClicked = character;
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
                SpawnController.Instance.EquipCPU(character.scriptableStats);
            }
            else
            {
                StartCoroutine(MaxMessageRoutine());
                return;
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
            partySlotsGameplayUI[i].enabled = false;
            partySlotsGameplayUI[i].sprite = null;
            characterPrice[i].enabled = false;

        }

        for (int i = 0; i < party.Count; i++)
        {
            partySlots[i].enabled = true;
            partySlots[i].sprite = party[i].headshot;
            partySlotsGameplayUI[i].enabled = true;
            partySlotsGameplayUI[i].sprite = party[i].headshot;
            characterPrice[i].enabled = true;
            characterPrice[i].text = party[i].scriptableStats._spawnCost.ToString(); // crazy ahh line right here
        }
    }
}
