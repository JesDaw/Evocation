using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelect : MonoBehaviour
{
    [SerializeField] GameObject characterSelectMenu;

    [SerializeField] TMP_Text characterNameText;
    [SerializeField] TMP_Text characterDescriptionText;
    [SerializeField] Image characterImage;

    private bool menuOpen = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            menuOpen = !menuOpen;
            characterSelectMenu.SetActive(menuOpen);
        }
    }

    public void showCharacterInfo(CharacterData character)
    {
        characterImage.sprite = character.portrait;
        characterNameText.text = character.characterName;
        characterDescriptionText.text = character.description;
    }
}
