using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpdateMagicUIFunctions : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI totalMana;
    [SerializeField] TextMeshProUGUI cost;

    public void UpdateSpellGUI(PlayerSpells _playerSpell)
    {
        cost.text = _playerSpell.Cost.ToString();
    }

    public void UpdateTotalMana(int _totalMana, int _changedMana)
    {
        totalMana.text = "Total Mana: " + _totalMana;
    }
}
