using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stats", menuName = "Stats", order = 0)]
public class ScriptableStats : ScriptableObject
{
    public List<string> _CpuPriority;
    public Sprite _Sprite;
    public string _Clan;
    public int _MaxHealth = 1;
    public int _CurrentHealth = 1;
    public int _AttackDamage;
    public int _AttackStartup;
    public int _AttackActiveDuration;
    public int _AttackEndlag;
    public float _MoveSpeed;
    public float _StopDistance;
    public float _KnockBackMax = 1;
    public float _KnockBackHealth;
    public float _KnockBackVelocity;
    public int _spawnCost;
    public List<StatusEffect> _StatusEffects;
    //x = Tick
    //y = Length
    public List<StatusEffect> _EffectsToApply;
    public List<Vector2> _StatusTicksMax;
    public List<Vector2> _StatusTicks;
    public int _StatusMax;
    public int _StatusHealth;
    public AttackType _attackType;
}
    
