using UnityEngine;
using UnityEngine.UI;
using TMPro;
// make this singleton for easy referance
public class UpdateMagicUIFunctions : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI totalMana;
    [SerializeField] TextMeshProUGUI cost;
    [SerializeField] Slider ManaBarImage;
    void Start()
    {
        ManaBarImage.value = 0;
    }

    public void UpdateSpellGUI(PlayerSpells _playerSpell)
    {
        cost.text = _playerSpell.Cost.ToString();
    }

    public void UpdateTotalMana(int _totalMana, int _changedMana)
    {
        totalMana.text = _totalMana.ToString();
        ManaBarImage.value = (float)_totalMana/100;        
    }
}
