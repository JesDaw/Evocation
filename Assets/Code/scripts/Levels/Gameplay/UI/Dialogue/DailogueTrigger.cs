using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DailogueTrigger : MonoBehaviour
{
    public List<Dialogue> Slides = new List<Dialogue>(); 
    DialogueManager dialogueManager;
    [SerializeField] public UltEvents.UltEvent[] EndOfLines;

    void Start()
    {
        dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (EndOfLines.Length <= 0) Debug.LogWarning($"[DailogueTrigger] No end of line events declared on {gameObject.name}");
    }
    public void TriggerDailogue(InputAction.CallbackContext context)
    {
        if (!context.started || dialogueManager.DialogueActive) return;
        TriggerDailogue();
    }
    
    public void TriggerDailogue()
    {
        dialogueManager.StartDialogue(Slides, this);
    }
    
    public void EndDialogue(int eventToCall) // either close the dialog box here or 
    { 
        EndOfLines[eventToCall]?.Invoke();
    }
}
