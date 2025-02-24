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
    public float _KnockBackMax;
    public float _KnockBackHealth;
    public float _KnockBackVelocity;
    public List<StatusEffect> _StatusEffects;
    [SerializeField] UnityEvent OnDeath, OnDamage;
    [SerializeField] UnityEvent<Vector2> OnKnocked;

    public void Attack(int _Damage)
    {
        _Health -= _Damage;
        _KnockBackHealth -= _Damage;

        OnDamage.Invoke();
        if(_Health <= 0)
        {
            OnDeath.Invoke();
            Destroy(gameObject);
        }

        if(_KnockBackHealth <= 0)
        {
            _KnockBackHealth = _KnockBackMax;
            OnKnocked.Invoke(new Vector2(-1 * _KnockBackVelocity, _KnockBackVelocity));
        }
    }

    public void AddStatusEffect(StatusEffect _effect)
    {
        _StatusEffects.Add(_effect);
    }

    //IEnumerator ApplyStatus(StatusEffect _effect)
    //{
        //yield return new WaitForSeconds(_effect.Length);
    //}
}
    
