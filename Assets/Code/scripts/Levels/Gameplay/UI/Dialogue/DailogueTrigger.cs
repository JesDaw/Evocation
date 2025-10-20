using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DailogueTrigger : MonoBehaviour
{
    public List<Dialogue> Slides = new List<Dialogue>(); 
    DialogueManager dialogueManager;
    [SerializeField] internal UltEvents.UltEvent EndOfLines;

    void Start()
    {
        dialogueManager = FindFirstObjectByType<DialogueManager>();
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
    
    public void EndDialogue()
    {
        EndOfLines?.Invoke();
    }
}
