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
    public string ODS;
    public string RPS_Type;
    [TextArea(2, 5)] public string OtherNotes;

    [Header("Pushing Power")]
    [Range(0, 100)] public float _MoveSpeed;
    [Range(0, 100)] public float _KnockBackDamage;

    [Header("Damage Per Second")]
    [Range(0, 100)] public int _AttackDamage;
    [Range(0, 100)] public float _AttackEndlag;

    [Header("Defense")]
    public int _MaxHealth = 1;
    public float _KnockBackMaxHealth = 1;
    [Range(0, 100)] public float _HorizontalRange = 2f;

    [Header("Combat Configuration")]
    public AttackStyle _AttackStyle;
    public bool _IsAOE;
    [Range(1, 100)] public int _MaxAOETargets = 5;

    [Header("Knockback Physics")]
    [Range(0, 100)] public float _KnockBackVelocity = 10f;
    [Range(0, 90)] public float _KnockBackAngle = 45f;
    [Range(0, 100)] public float _VerticalRange = 2f;

    [Header("Projectile Settings")]
    public GameObject _ProjectilePrefab;
    [Range(0, 100)] public float _ProjectileSpeed = 15f;
    [Range(0, 100)] public float _ProjectileMaxHeight = 2f;

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
    [Range(0, 10)] public float _AnimationMoveSpeed = 1f;

    public void RefreshBalancing(
    float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float wAOE,
    float avgHP, float avgKBMaxHealth, float avgMoveSpeed,
    float spaceTarget)
    {
        float gravity  = 9.81f;
        float angleRad = _KnockBackAngle * Mathf.Deg2Rad;
        float moveSpeed = _MoveSpeed * wMove;
        float endlag    = Mathf.Max(_AttackEndlag * wEnd, 0.01f);
        float hitsPerSecond = 1f / (endlag + 0.5f); // kept for display pillars

        // --- OUR KNOCKBACK PHYSICS ---
        float kbRatio    = (_KnockBackDamage * wKB_Dmg) / 100f;
        float vEff       = _KnockBackVelocity * kbRatio;
        float airTime    = (2f * vEff * Mathf.Sin(angleRad)) / gravity;
        float kbDistance = (Mathf.Pow(vEff, 2f) * Mathf.Sin(2f * angleRad)) / gravity;

        // --- DISPLAY PILLARS (inspector readability) ---
        float aoeMult      = _IsAOE ? (1f + (_MaxAOETargets - 1) * wAOE) : 1f;
        float rangeBonus   = 1f + (_HorizontalRange * 0.1f * wRange);
        float kbResistance = 1f + (_KnockBackMaxHealth * 0.05f * wKB_HP);
        _Calc_DPS     = (_AttackDamage * wAtk * hitsPerSecond * aoeMult) * rangeBonus;
        _Calc_Defense = (_MaxHealth * wHP) * kbResistance;

        // --- SIMULATION: time to claim spaceTarget units against the average enemy ---

        float effectiveAtkDmg = Mathf.Max(_AttackDamage * wAtk * aoeMult * rangeBonus, 0f);
        float effectiveKBDmg  = _KnockBackDamage * wKB_Dmg;

        // How many of OUR hits does it take to kill / KB the average enemy?
        float hitsToKill = (effectiveAtkDmg > 0f) ? (avgHP / effectiveAtkDmg)                         : float.MaxValue;
        float hitsToKB   = (effectiveKBDmg  > 0f) ? Mathf.Max(avgKBMaxHealth / effectiveKBDmg, 1f) : float.MaxValue;

        float killTime     = (hitsToKill < float.MaxValue) ? hitsToKill * endlag : float.MaxValue;
        float kbAttackTime = (hitsToKB   < float.MaxValue) ? hitsToKB   * endlag : float.MaxValue;

        // After a knockback: both units walk toward each other to re-engage
        float combinedSpeed = moveSpeed + avgMoveSpeed;
        float approachTime  = (combinedSpeed > 0f && kbDistance > 0f) ? kbDistance / combinedSpeed : 0f;

        // A full KB cycle = wind-up attacks + enemy airborne + enemy walks back
        float kbCycleTime     = (kbAttackTime < float.MaxValue) ? kbAttackTime + airTime + approachTime : float.MaxValue;
        float spacePerKBCycle = moveSpeed * (airTime + approachTime); // we walk the whole time enemy is gone

        // Store KB space rate for display
        _Calc_PushingPower = (kbCycleTime > 0f && kbCycleTime < float.MaxValue)
            ? spacePerKBCycle / kbCycleTime
            : 0f;

        // How many full KB cycles fit inside the kill window?
        float numKBCycles     = (kbCycleTime < float.MaxValue && killTime < float.MaxValue && kbCycleTime > 0f)
            ? killTime / kbCycleTime
            : 0f;
        float spaceDuringKill = numKBCycles * spacePerKBCycle;

        // --- DETERMINE TIME TO REACH spaceTarget ---
        bool canKill = effectiveAtkDmg > 0f;
        bool canKB   = effectiveKBDmg  > 0f && spacePerKBCycle > 0f && kbCycleTime < float.MaxValue;

        float timeToTarget;
        float remainingSpace = spaceTarget - spaceDuringKill;

        if (!canKill && !canKB)
        {
            // Unit can neither kill nor push — useless
            timeToTarget = float.MaxValue;
        }
        else if (!canKill)
        {
            // Pure KB pusher: sweeps the enemy back forever at a fixed rate
            float kbSpaceRate = spacePerKBCycle / kbCycleTime;
            timeToTarget = (kbSpaceRate > 0f) ? spaceTarget / kbSpaceRate : float.MaxValue;
        }
        else if (remainingSpace <= 0f)
        {
            // Enough space was claimed through KB alone before the kill landed
            timeToTarget = killTime;
        }
        else if (moveSpeed > 0f)
        {
            // Kill + walk freely through the remaining space
            timeToTarget = killTime + remainingSpace / moveSpeed;
        }
        else
        {
            // Kills but is immobile — can never reach spaceTarget
            timeToTarget = float.MaxValue;
        }

        _CalculatedPower  = (timeToTarget > 0f && timeToTarget < float.MaxValue)
            ? spaceTarget / timeToTarget
            : 0f;

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