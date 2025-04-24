using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthbar : MonoBehaviour
{
    [SerializeField] ActivePlayer ActivePlayer;
    [SerializeField] Slider _Slider;
    void Start()
    {
        UpdateHealth();
    }

    public void UpdateHealth()
    {
        if (ActivePlayer.CurrentPlayer.TryGetComponent<Stats>(out Stats stats))
        {
            _Slider.maxValue = stats._Health;
            _Slider.value = stats._Health;
        }
        else
        {
            Debug.LogWarning("Stats component not found on CurrentPlayer!");
        }
    }
}
