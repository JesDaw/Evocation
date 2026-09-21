using UnityEngine;

public class DiologueChoiceWrapper : MonoBehaviour
{
    [HideInInspector] public int ChoiceIndex = 0;
    public void SelectChoice()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning($"[{this}.DiologueChoiceWrapper] can't find DialogueManager");

        }

        DialogueManager.Instance.EndDialogue(ChoiceIndex);
    }
}
