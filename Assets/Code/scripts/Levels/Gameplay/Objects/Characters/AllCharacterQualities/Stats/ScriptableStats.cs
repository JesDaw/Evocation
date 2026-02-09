using System.Collections.Generic;
using UnityEngine;
using Evocation.Clans;

[CreateAssetMenu(fileName = "CpuStats", menuName = "CPU/Stats", order = 0)]
public class ScriptableStats : ScriptableObject
{
    [Header("Animation & Appearance")]
    [HideInInspector] public bool _Rotate;
    [HideInInspector] public AnimatorOverrideController _animator;
    [HideInInspector] public animationRigs[] _Sprites;
    public float _AnimationMoveSpeed = 1f;

    [Space]
    [Header("General Stats")]
    public int _MaxHealth = 1;
    public float _MoveSpeed;
    public int _spawnCost;

    [Header("Combat Configuration")]
    public AttackStyle _AttackStyle;    
    public bool _IsAOE; 
    public int _MaxAOETargets = 5;

    [Header("Combat Stats")]
    public int _AttackDamage;
    public float _AttackEndlag;
    
    [Header("Range Settings")]
    public float _HorizontalRange = 2f; 
    public float _VerticalRange = 2f;

    [Header("Projectile Settings")]
    public GameObject _ProjectilePrefab;
    public float _ProjectileSpeed = 15f;
    public float _ProjectileMaxHeight = 2f;
    
    [Header("Projectile Curves")]
    public AnimationCurve _TrajectoryCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0)); 
    public AnimationCurve _AxisCorrectionCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve _SpeedCurve = AnimationCurve.Constant(0, 1, 1);

    [Header("Status Effects")]
    public List<StatusEffect> _EffectsToApply = new List<StatusEffect>();

    [Space]
    [Header("Knockback")]
    public float _KnockBackMaxHealth = 1;
    public float _KnockBackVelocity;
}

public enum AttackStyle
{
    Melee,
    Projectile
}

[System.Serializable]
public class animationRigs
{
    public enum animationKey { Idle, Running, Attack, Knockback }
    public animationKey Key;
    public GameObject Rig;
    public Vector2 Offset;
}