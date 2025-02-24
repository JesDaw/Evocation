using UnityEngine;
using UnityEngine.UI;

public class CpuHealthBar : MonoBehaviour
{
    [SerializeField] Stats _Stats;
    [SerializeField] Slider _Slider;
    void Start()
    {
        _Slider.gameObject.SetActive(false);
    }
    public void SetHealth()
    {
        _Slider.maxValue = _Stats._Health;
    }

    public void UpdateHealth()
    {
        _Slider.gameObject.SetActive(true);
        _Slider.value = _Stats._Health;
    }
}
