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
    [Tooltip("Only used when the slide has a Presentation Style. Off: the sentence goes below the previous one.")]
    public bool useScreenPosition;
    [Tooltip("Top-left corner of the sentence inside the Sentence Container: (0, 0) is top-left, (1, 1) is bottom-right.")]
    public Vector2 screenPosition;
    public CharacterDialogueInfo CharacterOverride; 
    public int CharacterBody;
    public int CharacterFace;
    public UltEvents.UltEvent LineStartEvent;
    public UltEvents.UltEvent LineEndEvent;
}