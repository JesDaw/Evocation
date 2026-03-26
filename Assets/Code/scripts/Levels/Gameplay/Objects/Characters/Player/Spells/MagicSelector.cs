using UnityEngine;

public class MagicSelector : MonoBehaviour
{
    [SerializeField] SpellsManager spellManager;

    private void OnTriggerEnter2D(Collider2D _other)
    {
        Transform t = _other.transform;

        if (!spellManager.CurrentlySelected.Contains(t))
        {
            spellManager.CurrentlySelected.Add(t);
        }
    }

    private void OnTriggerExit2D(Collider2D _other)
    {
        Transform t = _other.transform;

        if (spellManager.CurrentlySelected.Contains(t))
        {
            spellManager.CurrentlySelected.Remove(t);
        }
    }
}
