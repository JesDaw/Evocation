using UnityEngine;
using TMPro;


[CreateAssetMenu(fileName = "CharacterDialogueInfo", menuName = "Character Select/CharacterDialogueInfo")]
public class CharacterDialogueInfo : ScriptableObject
{
    public string CharacterName;
    public Color nameColor;
    public Color textColor;
    public string Voice = "dialogueType"; 
    [Range(.0001f, 1f)] public float TextSpeed = .05f; 
    [Header("Font")]
    public TMP_FontAsset fontAsset;
    public float fontSize = 36f;

}