using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDialogueInfo", menuName = "Character Select/CharacterDialogueInfo")]
public class CharacterDialogueInfo : ScriptableObject
{
    public string CharacterName;
    public Color nameColor;
    public Color textColor;
    public AudioClip Voice;
    public float TextSpeed;
}
