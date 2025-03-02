using UnityEngine;
using UnityEngine.Events;

public class BuildingHealth : MonoBehaviour
{
    [SerializeField] FloatVariable Health;
   [SerializeField] UnityEvent _end_game;
    [SerializeField] UnityEvent _damage_taken;
   
    public void TakeDamage(float damage)
    {
        Health._Value -= damage;
        _damage_taken.Invoke();

        if (Health._Value <= 0)
        {
            Health._Value = 0;
            _end_game.Invoke();    
        }
    }
}
