using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpdateMagicUIFunctions : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI totalMana;
    [SerializeField] TextMeshProUGUI spellName;
    [SerializeField] TextMeshProUGUI cost;
    [SerializeField] TextMeshProUGUI radius;

    public void UpdateSpellGUI(PlayerSpells _playerSpell)
    {
        spellName.text = _playerSpell.SpellName;
        cost.text = _playerSpell.Cost.ToString();
        radius.text = _playerSpell.Radius.ToString();
    }

    public void UpdateTotalMana(int _totalMana, int _changedMana)
    {
        totalMana.text = "TotalMana: " + _totalMana;
    }
}
