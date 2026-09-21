using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpdateMagicUIFunctions : MonoBehaviour
{
    public static UpdateMagicUIFunctions Instance { get; private set; }

    [SerializeField] TextMeshProUGUI totalMana;
    [SerializeField] TextMeshProUGUI cost;
    [SerializeField] Slider ManaBarImage;
    [SerializeField] Image spellIconHolder; 

    void Awake() => Instance = this;

    void Start()
    {
        ManaBarImage.value = 0;
    }

    public void UpdateSpellGUI(SpellDefinition _playerSpell)
    {
        cost.text = _playerSpell.Cost.ToString();
        spellIconHolder.sprite = _playerSpell.Icon;
    }

    public void UpdateTotalMana(int _totalMana)
    {
        totalMana.text = _totalMana.ToString();
        ManaBarImage.value = (float)_totalMana / 100;
    }
}