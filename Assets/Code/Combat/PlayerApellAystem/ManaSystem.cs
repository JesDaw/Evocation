using UnityEngine;
using UnityEngine.Events;
public class ManaSystem : MonoBehaviour
{
    public static ManaSystem Instance;

    void Awake()
    {
        Instance = this;
    }

    [field: SerializeField]
    public int PlayerMana {private set; get;}
    public UnityEvent OnManaChanged;

    public void IncreaseMana(int _amt)
    {
        PlayerMana += _amt;
        UpdateMagicUIFunctions.Instance.UpdateTotalMana(PlayerMana);
        OnManaChanged?.Invoke();
    }

    public bool SpendMana(uint _amt)
    {
        if(PlayerMana >= _amt)
        {
            PlayerMana -= (int)_amt;
            UpdateMagicUIFunctions.Instance.UpdateTotalMana(PlayerMana);
            OnManaChanged?.Invoke();
            return true;
        }

        return false;
    }
}
