using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSelect : MonoBehaviour
{
    [SerializeField]
    private GameObject characterSelectMenu;

    [SerializeField] 
    InputActionAsset inputActions;

    private InputAction characterSelectAction;
    private bool menuOpen = false;

    private void Start()
    {
        characterSelectAction = inputActions.FindAction("ToggleCharacterSelect");

        characterSelectAction.performed += OnToggleCharacterSelect;
        characterSelectAction.Enable();

        characterSelectMenu.SetActive(false);
    }

    private void OnToggleCharacterSelect(InputAction.CallbackContext context)
    {
        menuOpen = !menuOpen;
        characterSelectMenu.SetActive(menuOpen);
    }

    private void OnDestroy()
    {
        if (characterSelectAction != null)
        {
            characterSelectAction.performed -= OnToggleCharacterSelect;
        }
    }
}