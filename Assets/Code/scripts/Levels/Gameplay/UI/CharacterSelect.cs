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

    CharacterData lastClicked = null;

    void Start()
    {
        maxMessage.SetActive(false);
        UpdatePartyUI();
    }

    public void ShowCharacterInfo(CharacterData character)
    {
        characterImage.sprite = character.portrait;
        characterNameText.text = character.characterName;
        characterDescriptionText.text = character.description;
    }

    public void OnCharacterClicked(CharacterData character)
    {
        //first click shows the character info only
        if (lastClicked != character)
        {
            lastClicked = character;
            ShowCharacterInfo(character);
            return;
        }

        //second click toggle the character in/out of party
        lastClicked = null;

        if (party.Contains(character))
        {
            party.Remove(character);
        }
        else
        {
            if (party.Count < partySize)
                party.Add(character);
            else
            {
                StartCoroutine(MaxMessageRoutine());
                return;
            }
        }

        UpdatePartyUI();
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
        }

        for (int i = 0; i < party.Count; i++)
        {
            partySlots[i].enabled = true;
            partySlots[i].sprite = party[i].portrait;
        }
    }
}
