using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpdateMagicUIFunctions : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI totalMana;
    [SerializeField] TextMeshProUGUI cost;
    [SerializeField] Slider ManaBarImage;

    public void UpdateSpellGUI(PlayerSpells _playerSpell)
    {
        cost.text = _playerSpell.Cost.ToString();
    }

    public void UpdateTotalMana(int _totalMana, int _changedMana)
    {
        totalMana.text = _totalMana.ToString();
        ManaBarImage.value = _totalMana/100;
        Debug.Log("here" + _totalMana);
        
    }
}
