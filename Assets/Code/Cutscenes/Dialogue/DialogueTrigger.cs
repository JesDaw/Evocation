using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour // multiple pages
{
    public List<Dialogue> Slides = new List<Dialogue>(); 
    [SerializeField] public DialogueChoice[] EndOfLines;
    public UltEvents.UltEvent DefultEvent;
    [Tooltip("Fired by <?Name> tags in this trigger's lines, at that exact character (e.g. <?door>). " +
        "face, body and sound are built in and handled by DialogueManager.")]
    public List<DialogueTextEvent> TextEvents = new List<DialogueTextEvent>();

    void Start()
    {
        /*foreach (var slide in Slides)
        {
            foreach (var line in slide.Lines) 
            {
                if (line.alternitiveTextBox == null) continue;
                if (!string.IsNullOrEmpty(line.alternitiveTextBox.text)) line.Line = line.alternitiveTextBox.text; // breaks if there are multiple lines with same text box
                line.alternitiveTextBox.text = "";
            }
        } */ 
    }
    public void TriggerDailogue(InputAction.CallbackContext context)
    {
        if (!context.started || DialogueManager.Instance.DialogueActive) return;
        TriggerDailogue();
    }
    
    public void TriggerDailogue()
    {
        DialogueManager.Instance.StartDialogue(Slides, this);
    }
    public void EndDialogueDefultEvent() 
    { 
        DefultEvent?.Invoke();
    }
    
    public void EndDialogue(int eventToCall)
    {
        if (eventToCall < 0 || eventToCall >= EndOfLines.Length) return;
        EndOfLines[eventToCall].ultEvent?.Invoke();
    }

    public bool TryInvokeTextEvent(string eventName)
    {
        foreach (DialogueTextEvent textEvent in TextEvents)
        {
            if (!string.Equals(textEvent.Name, eventName, System.StringComparison.OrdinalIgnoreCase)) continue;
            textEvent.Event?.Invoke();
            return true;
        }
        return false;
    }
}
[System.Serializable]
public class DialogueChoice
{
    public string Text = "null";
    public UltEvents.UltEvent ultEvent;
}
[System.Serializable]
public class DialogueTextEvent
{
    public string Name;
    public UltEvents.UltEvent Event;
}
