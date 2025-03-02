using UnityEngine;
using UnityEngine.Events;

public class BuildingHealth : MonoBehaviour
{
    [SerializeField] FloatVariable Health;
   [SerializeField] UnityEvent _end_game;
   
    public void TakeDamage(float damage)
    {
        Health._Value -= damage;

        if (Health._Value <= 0)
        {
            Health._Value = 0;
            _end_game.Invoke();    
        }
    }
}
