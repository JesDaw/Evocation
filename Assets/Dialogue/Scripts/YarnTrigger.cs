using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Starts a Yarn node, the way DialogueTrigger starts dialogue written in the scene.
// Call TriggerDialogue from a Timeline Signal Receiver, an UltEvent or an input action.
public class YarnTrigger : MonoBehaviour
{
    [Tooltip("The Yarn node to run: the 'title:' at the top of a node in a .yarn file.")]
    [SerializeField] string startNode = "Start";
    [Tooltip("Runs when the dialogue started by this trigger finishes.")]
    public UltEvents.UltEvent DialogueEnded;
    [Tooltip("Run from Yarn with <<event Name>> on its own line, or <?Name> inside a line (fires at that letter). " +
        "Use them to play Timelines, move objects, etc.")]
    public List<DialogueTextEvent> Events = new List<DialogueTextEvent>();
    [Tooltip("Start the dialogue as soon as the scene starts.")]
    [SerializeField] bool triggerOnStart = false;

    void Start()
    {
        if (triggerOnStart) TriggerDialogue();
    }

    public void TriggerDialogue(InputAction.CallbackContext context)
    {
        if (!context.started || DialogueManager.Instance.DialogueActive) return;
        TriggerDialogue();
    }

    public void TriggerDialogue()
    {
        if (YarnDialoguePresenter.Instance == null)
        {
            Debug.LogError($"[YarnTrigger] {name}: no YarnDialoguePresenter found; it belongs on the DialogueManager prefab.", this);
            return;
        }
        YarnDialoguePresenter.Instance.StartDialogue(startNode, this);
    }

    public bool TryInvokeEvent(string eventName)
    {
        foreach (DialogueTextEvent dialogueEvent in Events)
        {
            if (!string.Equals(dialogueEvent.Name, eventName, System.StringComparison.OrdinalIgnoreCase)) continue;
            dialogueEvent.Event?.Invoke();
            return true;
        }
        return false;
    }
}
