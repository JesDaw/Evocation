using TMPro;
using UnityEngine;

[System.Serializable]
public class Dialogue // full pages
{
    public CharacterDialogueInfo Character;
    public DialogueLine[] Lines;
    public CanvasGroup alternitiveCanvasGroup;
    [Tooltip("Optional. Lines without an Alternitive Text Box become separate sentences on screen " +
        "(spawned under the DialogueManager's Sentence Container) instead of using the dialogue box.")]
    public DialoguePresentationStyle presentationStyle;

    
    
}

[System.Serializable]
public class DialogueLine //single sentances
{
    [TextArea(3, 10)] public string Line;
    public TMP_Text alternitiveTextBox;
    public CharacterDialogueInfo CharacterOverride; 
    public int CharacterBody;
    public int CharacterFace;
    public UltEvents.UltEvent LineStartEvent;
    public UltEvents.UltEvent LineEndEvent;
}