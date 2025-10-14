using UnityEngine;
using UnityEngine.InputSystem;

public class DailogueTrigger : MonoBehaviour
{
    public DialogueSO dialogueSO;
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
        dialogueManager.StartDialogue(dialogueSO, this);
        dialogueManager.DialogueActive = true;
    }
    
    public void EndDialogue()
    {
        dialogueManager.DialogueActive = false;
        EndOfLines?.Invoke();
    }
}
