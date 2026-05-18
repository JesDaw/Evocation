using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DailogueTrigger : MonoBehaviour
{
    public List<Dialogue> Slides = new List<Dialogue>(); 
    [SerializeField] public DialogueChoice[] EndOfLines;
    public UltEvents.UltEvent DefultEvent;

    void Start()
    {
        //if (EndOfLines.Length <= 0) Debug.LogWarning($"[DailogueTrigger] No end of line events declared on {gameObject.name}");
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
        EndOfLines[eventToCall].ultEvent?.Invoke();
    }
}
[System.Serializable]
public class DialogueChoice
{
    public string Text = "null";
    public UltEvents.UltEvent ultEvent;
}
