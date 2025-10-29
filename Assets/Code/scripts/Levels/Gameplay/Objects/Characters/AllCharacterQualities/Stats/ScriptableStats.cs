using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stats", menuName = "Stats", order = 0)]
public class ScriptableStats : ScriptableObject
{
    [Header("Clan")]
    public string _Clan;
    public List<string> _CpuPriority;
    [Space]
    [Header("Animation")]
    public AnimatorOverrideController _animator;
    public animationRigs[] _Sprites;
    [Space]
    [Header("General Info")]
    public int _MaxHealth = 1;
    public int _CurrentHealth = 1;
    public int _AttackDamage;
    public int _AttackStartup;
    public int _AttackActiveDuration;
    public int _AttackEndlag;
    public AttackType _attackType;
    public float _MoveSpeed;
    public float _StopDistance;
    public float _KnockBackMax = 1;
    public float _KnockBackHealth;
    public float _KnockBackVelocity;
    public int _spawnCost;
    [Space]
    [Header("Status Effects")]
    public List<StatusEffect> _StatusEffects;
    //x = Tick
    //y = Length
    public List<StatusEffect> _EffectsToApply;
    public List<Vector2> _StatusTicksMax;
    public List<Vector2> _StatusTicks;
    public int _StatusMax;
    public int _StatusHealth;
}

[System.Serializable]
public class animationRigs
{
    public enum animationKey {Idle, Running, Attack, Knockback};
    public animationKey Key;
    public GameObject Rig;
    public Vector2 Offset;
}
    
