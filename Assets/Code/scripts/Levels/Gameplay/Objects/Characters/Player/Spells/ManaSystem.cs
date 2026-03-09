using UnityEngine;

public class ManaSystem : MonoBehaviour
{
    [field: SerializeField]
    public int PlayerMana {private set; get;}

    public void IncreaseMana(int _amt) =>
        PlayerMana += _amt;

    public bool SpendMana(uint _amt)
    {
        if(PlayerMana >= _amt)
        {
            PlayerMana -= (int)_amt;
            return true;
        }

        return false;
    }
}
