using UnityEngine;

public class CharacterButton : MonoBehaviour
{
    public CharacterData character;
    public CharacterSelect selector;

    public void OnClick()
    {
        selector.OnCharacterClicked(character);
    }
}
