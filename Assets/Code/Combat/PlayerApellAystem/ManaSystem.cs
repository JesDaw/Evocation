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
    [Tooltip("First one is total, second is amount changed")] // why are we tracking amount changed here?
    public UnityEvent<int, int> OnManaChanged;

    public void IncreaseMana(int _amt)
    {
        PlayerMana += _amt;
        OnManaChanged.Invoke(PlayerMana, _amt);
    }

    public bool SpendMana(uint _amt)
    {
        if(PlayerMana >= _amt)
        {
            PlayerMana -= (int)_amt;
            OnManaChanged.Invoke(PlayerMana, (int)_amt);
            return true;
        }

        return false;
    }
}
