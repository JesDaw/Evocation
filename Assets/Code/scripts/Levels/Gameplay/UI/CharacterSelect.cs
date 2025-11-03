using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelect : MonoBehaviour
{
    [SerializeField] private GameObject characterSelectMenu;

    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text characterDescriptionText;
    [SerializeField] private Image characterImage;

    private bool menuOpen = false;

    private void Start()
    {
        characterSelectMenu.SetActive(false);
    }

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
