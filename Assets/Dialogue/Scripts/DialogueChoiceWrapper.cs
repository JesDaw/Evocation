using UnityEngine;

public class DialogueChoiceWrapper : MonoBehaviour
{
    [HideInInspector] public int ChoiceIndex = 0;
    public void SelectChoice()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning($"[{this}.DialogueChoiceWrapper] can't find DialogueManager");
            return;
        }

        DialogueManager.Instance.SelectChoice(ChoiceIndex);
    }
}
