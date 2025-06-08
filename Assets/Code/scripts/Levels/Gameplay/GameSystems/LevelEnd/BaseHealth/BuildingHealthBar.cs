using UnityEngine;
using UnityEngine.UI;

public class BuildingHealthBar : MonoBehaviour
{
    [SerializeField] FloatVariable _health;
    [SerializeField] Slider _Slider;
    [SerializeField] float _MaxHealth;
    void Start()
    {
        _Slider.gameObject.SetActive(false);
        _MaxHealth = _health._Value;
        _Slider.maxValue = _MaxHealth;
    }

    public void UpdateHealth()
    {
        _Slider.value = _health._Value;
        if (_Slider.value != _MaxHealth)
        {
            _Slider.gameObject.SetActive(true);
        }
        else
        {
            _Slider.gameObject.SetActive(false);
        }
    }
}
