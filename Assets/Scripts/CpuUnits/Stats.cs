using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Stats : MonoBehaviour
{
    public List<string> _CpuPriority;
    public string _Clan;
    public int _Health = 1;
    public int _Attack;
    public float _AttackSpeed;
    public float _Speed;
    public float _StopDistance;
    [SerializeField] UnityEvent OnDeath;
    [SerializeField] UnityEvent OnDamageOther;

    public void Attack(int _Damage)
    {
        OnDamageOther.Invoke();

        _Health -= _Damage;
        if(_Health <= 0)
        {
            OnDeath.Invoke();
            Destroy(gameObject);
        }
    }
}
    
