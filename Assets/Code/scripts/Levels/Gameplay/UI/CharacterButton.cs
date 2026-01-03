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
        headshotFrame.sprite = character.headshot;
    }

    public void OnClick()
    {
        selector.OnCharacterClicked(character);
    }
}
