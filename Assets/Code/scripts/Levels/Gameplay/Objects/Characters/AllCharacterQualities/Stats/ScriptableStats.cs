using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CpuStats", menuName = "CPU/Stats", order = 0)]
public class ScriptableStats : ScriptableObject
{
    [Header("--- COST & VALUE ---")]
    public int _spawnCost;
    public float _CalculatedPower; 
    public float _ValueDiscrepancy;

    [Header("Personality & Role")]
    public string Theme;
    public string ODS;
    public string RPS_Type;
    [TextArea(2, 5)] public string OtherNotes;

    [Header("Pushing Power")]
    [Range(0, 15)] public float _MoveSpeed; 
    [Range(0, 100)] public float _KnockBackDamage;

    [Header("Damage Per Second")]
    [Range(0, 100)] public int _AttackDamage;
    [Range(0, 30)] public float _AttackEndlag; 

    [Header("Defense")]
    public int _MaxHealth = 1;
    public float _KnockBackMaxHealth = 1;
    [Range(0, 30)] public float _VerticalRange = 2f;
    [Range(0, 30)] public float _HorizontalRange = 2f;

    [Header("Combat Configuration")]
    public AttackStyle _AttackStyle;
    public bool _IsAOE;
    [Range(1, 100)] public int _MaxAOETargets = 5;
    [Range(0, 100)] public float _KnockBackVelocity = 10f;
    [Range(0, 90)] public float _KnockBackAngle = 45f;

    [Header("Projectile Settings")]
    public GameObject _ProjectilePrefab;
    [Range(0, 100)] public float _ProjectileSpeed = 15f;
    [Range(0, 100)] public float _ProjectileMaxHeight = 2f;
    public AnimationCurve _TrajectoryCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    public AnimationCurve _AxisCorrectionCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve _SpeedCurve = AnimationCurve.Constant(0, 1, 1);

    [Header("Status Effects")]
    public List<StatusEffect> _EffectsToApply = new List<StatusEffect>();

    [Header("Visuals & Animation")]
    public bool _Rotate;
    public AnimatorOverrideController _animator;
    public animationRigs[] _Sprites;
    [Range(0, 10)] public float _AnimationMoveSpeed = 1f;
    public List<GameObject> vfx = new();
    public List<Vector2> vfxOffsets = new();
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