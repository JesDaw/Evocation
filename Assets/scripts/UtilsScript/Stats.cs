using System;
using System.Collections.Generic;
using System.Collections;
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
    //x = Tick
    //y = Length
    private List<Vector2> _StatusTicksMax;
    public List<Vector2> _StatusTicks;
    public int _StatusMax;
    public int _StatusHealth;
    [SerializeField] UnityEvent OnDeath, OnDamage, OnTick;
    [SerializeField] UnityEvent<Vector2> OnKnocked;

    public void Start()
    {
        StartCoroutine(StatusEffectLoop());
    }

    IEnumerator StatusEffectLoop()
    {
        if(_StatusEffects.Count == 0) StatusEffectLoop();

        //x = Tick
        //y = Length

        //upload cycle
        float _TickSpeed = 0.1f;

        yield return new WaitForSeconds(_TickSpeed);
        for(int I = 0; I < _StatusTicks.Count; I++)
        {
            Vector2 CurrentStatus = _StatusTicks[I];

            if(CurrentStatus.x > 0)
            {
                CurrentStatus.x -= _TickSpeed;
                Debug.Log(CurrentStatus);
            }
            else
            {
                CurrentStatus.x = _StatusTicksMax[I].x;
                Debug.Log(CurrentStatus);
                if(OnTick != null) OnTick.Invoke();
            }

            _StatusTicks[I] = CurrentStatus;
        }
        //(circular logic), there's prob a better way to do this
        //but i like this
        StartCoroutine(StatusEffectLoop());
    }

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
        _StatusTicks.Add(new Vector2(_effect._Tick, _effect._Length));
        _StatusTicksMax.Add(new Vector2(_effect._Tick, _effect._Length));
        _StatusHealth = _StatusMax;
    }
}
    
