using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : MonoBehaviour
{
    [HideInInspector] public CharacterData character;
    [SerializeField] Image headshotFrame;
    
    void Start ()
    {
        UpdateFrame();
    }

    public void UpdateCharacterDesplay()
    {
        CharacterSelect.Instance.UpdateCurrentDesplayedCharacter(character);
    }

    public void UpdateFrame()
    {
        headshotFrame.enabled = true;
        if (character != null) headshotFrame.sprite = character.headshot;
        else headshotFrame.enabled = false;
    }

    public void OnClick()
    {
        if (character == null) return;
        CharacterSelect.Instance.OnCharacterClicked(character);
    }
}
