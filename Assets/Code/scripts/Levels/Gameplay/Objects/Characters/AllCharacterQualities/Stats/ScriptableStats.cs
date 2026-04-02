using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CpuStats", menuName = "CPU/Stats", order = 0)]
public class ScriptableStats : ScriptableObject
{
    [Header("--- COST & VALUE ---")]
    public int _spawnCost;
    public float _CalculatedPower; 
    public float _ValueDiscrepancy;

    [Header("--- CALCULATED TOTALS (Automated) ---")]
    public float _Calc_PushingPower;
    public float _Calc_DPS;
    public float _Calc_Defense;

    [Header("Personality & Role")]
    public string Theme;
    public string ODS; // Offense, Defense, Support
    public string RPS_Type; // Rock, Paper, Scissors
    [TextArea(2, 5)] public string OtherNotes;

    [Header("Pushing Power")]
    public float _MoveSpeed;
    public float _KnockBackDamage;

    [Header("Damage Per Second")]
    public int _AttackDamage;
    public float _AttackEndlag;

    [Header("Defense")]
    public int _MaxHealth = 1;
    public float _KnockBackMaxHealth = 1;
    public float _HorizontalRange = 2f; 

    [Header("Other Modifiers")]
    public float _KnockBackVelocity;
    public float _VerticalRange = 2f;
    
    [Header("Combat Configuration")]
    public AttackStyle _AttackStyle;    
    public bool _IsAOE; 
    public int _MaxAOETargets = 5;

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

    [Header("Animation Driven VFX")] 
    public List<GameObject> vfx = new();
    public List<Vector2> vfxOffsets = new();

    [Header("Low Priority (Visuals)")]
    public bool _Rotate;
    public AnimatorOverrideController _animator;
    public animationRigs[] _Sprites;
    public float _AnimationMoveSpeed = 1f;

    public void RefreshBalancing(float wAtk, float wEndlag, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float wAOE)
    {
        // Pushing Power
        _Calc_PushingPower = (_MoveSpeed * wMove) + (_KnockBackDamage * wKB_Dmg);
        
        // DPS (Hyperbolic penalty for endlag)
        float baseDPS = (_AttackDamage * wAtk) / (Mathf.Max(_AttackEndlag * wEndlag, 0.01f) + 0.5f);
        float aoeMult = _IsAOE ? (1 + (_MaxAOETargets - 1) * wAOE) : 1f;
        _Calc_DPS = baseDPS * aoeMult;

        // Defense
        _Calc_Defense = (_MaxHealth * wHP) + (_KnockBackMaxHealth * wKB_HP) + (_HorizontalRange * wRange);

        // Resulting Power
        _CalculatedPower = _Calc_PushingPower + _Calc_DPS + _Calc_Defense;
        _ValueDiscrepancy = _CalculatedPower - _spawnCost;
    }
}

public enum AttackStyle { Melee, Projectile }

[System.Serializable]
public class animationRigs
{
    public enum animationKey { Idle, Running, Attack, Knockback }
    public animationKey Key;
    public GameObject Rig;
    public Vector2 Offset;
}