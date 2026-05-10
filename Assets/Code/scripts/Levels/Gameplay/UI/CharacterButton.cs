using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : MonoBehaviour
{
    public CharacterData character;
    public CharacterSelect selector;
    [SerializeField] Image headshotFrame;
    
    void Start ()
    {
        headshotFrame.enabled = true;
        if (character != null) headshotFrame.sprite = character.headshot;
        else headshotFrame.enabled = false;
    }

    public void OnClick()
    {
        if (character == null) return;
        selector.OnCharacterClicked(character);
    }
}
