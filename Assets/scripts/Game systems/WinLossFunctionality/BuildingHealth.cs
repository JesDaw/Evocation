using UnityEngine;
using UnityEngine.Events;

public class BuildingHealth : MonoBehaviour
{
    [SerializeField] FloatVariable Health;
    [SerializeField] UnityEvent _end_game;
    [SerializeField] UnityEvent ChangeMoneyGen;
    [SerializeField] UnityEvent _damage_taken;

   [SerializeField] bool MainBase;
   [SerializeField] bool MoneyBuilding;
   
    public void TakeDamage(float damage)
    {
        Health._Value -= damage;
        _damage_taken.Invoke();

        if (Health._Value <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Health._Value = 0;
        if(MainBase)
        {
            _end_game.Invoke();
        }
        else if(MoneyBuilding)
        {
            ChangeMoneyGen.Invoke();
            //_end_game.Invoke();
        }
    }

    public void ResetHealth(){ Health.Reset(); }
}
