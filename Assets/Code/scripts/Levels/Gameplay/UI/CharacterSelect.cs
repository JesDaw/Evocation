using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class CharacterSelect : MonoBehaviour
{
    [SerializeField]
    private GameObject characterSelectMenu;

    private InputSystemActions inputActions;
    
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text characterDescriptionText;
    [SerializeField] private Image characterImage;

    private bool menuOpen = false;

    private void Awake()
    {
        inputActions = new InputSystemActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.UI.ToggleCharacterSelect.performed += toggleCharacterSelect;
    }

    private void OnDisable()
    {
        inputActions.UI.ToggleCharacterSelect.performed -= toggleCharacterSelect;
        inputActions.Disable();
    }

    private void toggleCharacterSelect(InputAction.CallbackContext context)
    {
        menuOpen = !menuOpen;
        characterSelectMenu.SetActive(menuOpen);
    }
}