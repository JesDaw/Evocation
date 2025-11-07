using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelect : MonoBehaviour
{
    [SerializeField] TMP_Text characterNameText;
    [SerializeField] TMP_Text characterDescriptionText;
    [SerializeField] Image characterImage;

    public void showCharacterInfo(CharacterData character)
    {
        characterImage.sprite = character.portrait;
        characterNameText.text = character.characterName;
        characterDescriptionText.text = character.description;
    }
}
