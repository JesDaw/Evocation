using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Evocation.Clans;

public class Stats : MonoBehaviour
{
    public List<ClansList> _CpuPriority;
    public ClansList _Clan;

    //the above can be made into an enum, but i'll hold off on it until we get all the different clans
    public int _MaxHealth = 1;
    public float _CurrentHealth = 1;
    public float _AttackDamage;

    [Tooltip ("Only works on npc right now, but hopefully I can fully remove this when we have animatinos for the player")]
    public float _AttackStartup;
    public float _AttackEndlag;
    public float _MoveSpeed;
    public float _StopDistance;
    public float _KnockBackMax;
    public float _KnockBackHealth;
    public float _KnockBackVelocity;
    public int _spawnCost;
    public List<StatusEffect> _StatusEffects;
    //x = Tick
    //y = Length
    public List<Vector2> _StatusTicksMax;
    public List<Vector2> _StatusTicks;
    public int _StatusMax;
    public int _StatusHealth;
    [SerializeField] internal UltEvents.UltEvent OnDeath, OnDamage, OnKnocked;
    [SerializeField] internal UltEvents.UltEvent<bool> OnWitFlagDeath, OnWitFlagDamage;
    [SerializeField] UnityEvent<StatusEffect> OnTick;
    [SerializeField] bool _Invincible = false;
    [SerializeField] bool _DontDestroy = false;
    public bool _Enemy;
    DamageSource LastHitBy;

    public void ToggleInvinciblity(){ _Invincible = !_Invincible; }

    public void Start()
    {
        StartCoroutine(StatusEffectLoop());
        _CurrentHealth = _MaxHealth;
    }
    public void SetDestroyed(bool _ShouldDestroy)
    {
        _DontDestroy = _ShouldDestroy;
    }

    IEnumerator StatusEffectLoop()
    {
        if(_StatusEffects.Count == 0) StatusEffectLoop();

        //x = Tick
        //y = Length

        //upload cycle
        float _TickSpeed = 0.1f;

        yield return new WaitForSeconds(_TickSpeed);
        for (int I = 0; I < _StatusTicks.Count; I++)
        {
            Vector2 CurrentStatus = _StatusTicks[I];

            if (CurrentStatus.x > 0)
            {
                CurrentStatus.x -= _TickSpeed;
                //Debug.Log(CurrentStatus);
            }
            else
            {
                CurrentStatus.x = _StatusTicksMax[I].x;
                CurrentStatus.y -= CurrentStatus.x;

                DamageSource _statusEffect = new DamageSource(DamageSource.DamageType.StatusEffect);
                TakeDamage(_StatusEffects[I]._Damage);

                OnTick?.Invoke(_StatusEffects[I]);
            }

            _StatusTicks[I] = CurrentStatus;

            if (CurrentStatus.y < 0)
            {
                _StatusEffects.RemoveAt(I);
                _StatusTicks.RemoveAt(I);
                _StatusTicksMax.RemoveAt(I);
            }
        }
        //(circular logic), there's prob a better way to do this
        //but i like this
        StartCoroutine(StatusEffectLoop());
    }

    public void TakeDamage(float _Damage, DamageSource _AttackedBy = null)
    {
        if (_Invincible) return;

        _CurrentHealth -= _Damage;

        if(_AttackedBy != null && _AttackedBy.damageType == DamageSource.DamageType.StatusEffect)
            _KnockBackHealth--;

        if (_AttackedBy != null) OnWitFlagDamage.Invoke(_AttackedBy.IsEnemy);
        OnDamage.Invoke();

        if (_CurrentHealth <= 0)
        {
            Died();    
        }

        if (_KnockBackHealth <= 0)
        {
            _KnockBackHealth = _KnockBackMax;
            OnKnocked.Invoke();
        }

        LastHitBy = _AttackedBy;
    }
    public void Died()
    {
        if (LastHitBy != null)
        {
            Debug.Log(LastHitBy.IsEnemy);
            OnWitFlagDeath.Invoke(LastHitBy.IsEnemy);
        }
        OnDeath.Invoke();
        if (_DontDestroy) return;
        Destroy(gameObject);
    }
    public void SetHealth(int _Amount)
    {
        _CurrentHealth = _Amount;
    }

    public void AddStatusEffect(StatusEffect _effect)
    {
        _StatusEffects.Add(_effect);
        _StatusTicks.Add(new Vector2(_effect._Tick, _effect._Length));
        _StatusTicksMax.Add(new Vector2(_effect._Tick, _effect._Length));
        _StatusHealth = _StatusMax;
    }

    public AttackType attackType;

}

public class DamageSource
{
    //more context will be provided when I have time
    public DamageSource(){}
    public DamageSource(DamageType _damageType) {damageType = _damageType;}
    public bool IsEnemy;
    public DamageType damageType;
    public enum DamageType {StatusEffect}
}
    
