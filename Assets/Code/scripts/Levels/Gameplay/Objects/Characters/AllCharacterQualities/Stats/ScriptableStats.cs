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
    float avgHP = 50f, float avgKB_HP = 50f, float avgMove = 5f, float avgKB_Dmg = 10f,
    float avgAtk = 10f, float avgEndlag = 1f, float avgRange = 2f,
    float baseVelocity = 5f, float baseAngle = 45f) // <-- new
    {
        float g           = 9.81f;
        float avgAngleRad = baseAngle * Mathf.Deg2Rad; // <-- was hardcoded 45f
        float avgKBVel    = baseVelocity;              // <-- was hardcoded 10f

        // ── APPLY WEIGHTS TO CHARACTER A ──────────────────────────────────────────
        float A_HP     = _MaxHealth          * wHP;
        float A_KB_HP  = _KnockBackMaxHealth * wKB_HP;
        float A_Move   = _MoveSpeed          * wMove;
        float A_KB_Dmg = _KnockBackDamage    * wKB_Dmg;
        float A_Atk    = _AttackDamage       * wAtk;
        float A_Endlag = Mathf.Max(_AttackEndlag * wEnd, 0.01f);
        float A_Range  = _HorizontalRange    * wRange;
        float aoeMult  = _IsAOE ? Mathf.Max(_MaxAOETargets * wAOE, 1f) : 1f;
        A_Atk *= aoeMult;

        // ── ATTACK FREQUENCIES ────────────────────────────────────────────────────
        float FreqA = 1f / (A_Endlag + 0.5f);
        float FreqB = 1f / (Mathf.Max(avgEndlag, 0.01f) + 0.5f);

        // ── 1. FIGHT DURATION ─────────────────────────────────────────────────────
        float TTK_A     = (A_Atk  * FreqA) > 0f ? avgHP / (A_Atk  * FreqA) : float.MaxValue;
        float TTK_B     = (avgAtk * FreqB) > 0f ? A_HP  / (avgAtk * FreqB) : float.MaxValue;
        float TimeFight = Mathf.Max(Mathf.Min(TTK_A, TTK_B), 0.01f);

        // ── 2. FREE HITS FROM RANGE ADVANTAGE ────────────────────────────────────
        float FreeHitsA = (Mathf.Max(0f, A_Range - avgRange) / Mathf.Max(avgMove, 0.01f)) * FreqA;
        float FreeHitsB = (Mathf.Max(0f, avgRange - A_Range) / Mathf.Max(A_Move,  0.01f)) * FreqB;

        // ── 3. CHARACTER A KNOCKBACK PHYSICS ─────────────────────────────────────
        float angleRadA = _KnockBackAngle * Mathf.Deg2Rad;
        float kbRatioA  = A_KB_Dmg / Mathf.Max(avgKB_HP, 0.01f);
        float vEffA     = _KnockBackVelocity * kbRatioA;
        float D_KB_A    = (vEffA * vEffA * Mathf.Sin(2f * angleRadA)) / g;
        float AirTime_A = (2f * vEffA * Mathf.Sin(angleRadA)) / g;
        float t_meet_A  = (D_KB_A + (avgMove * AirTime_A) + (A_Move * A_Endlag))
                        / Mathf.Max(A_Move + avgMove, 0.01f);
        float D_gainA   = A_Move * Mathf.Max(t_meet_A - A_Endlag, 0f);

        // ── 4. CHARACTER B KNOCKBACK PHYSICS (uses grapher baselines) ────────────
        float kbRatioB  = (avgKB_Dmg * wKB_Dmg) / Mathf.Max(A_KB_HP, 0.01f);
        float vEffB     = avgKBVel * kbRatioB;           // avgKBVel = Base_Velocity
        float D_KB_B    = (vEffB * vEffB * Mathf.Sin(2f * avgAngleRad)) / g; // avgAngleRad = Base_Angle
        float AirTime_B = (2f * vEffB * Mathf.Sin(avgAngleRad)) / g;
        float t_meet_B  = (D_KB_B + (A_Move * AirTime_B) + (avgMove * avgEndlag))
                        / Mathf.Max(avgMove + A_Move, 0.01f);
        float D_gainB   = avgMove * Mathf.Max(t_meet_B - avgEndlag, 0f);

        // ── 5. TOTAL KNOCKBACKS ───────────────────────────────────────────────────
        float HTKB_A = Mathf.Max(avgKB_HP / Mathf.Max(A_KB_Dmg,  0.01f), 1f);
        float HTKB_B = Mathf.Max(A_KB_HP  / Mathf.Max(avgKB_Dmg, 0.01f), 1f);

        float TotalKB_A = ((TimeFight * FreqA) + FreeHitsA) / HTKB_A;
        float TotalKB_B = ((TimeFight * FreqB) + FreeHitsB) / HTKB_B;

        // ── 6. NET DISPLACEMENT & FINAL POWER ────────────────────────────────────
        float NetDisplacement = (TotalKB_A * D_gainA) - (TotalKB_B * D_gainB);
        float V_net           = NetDisplacement / TimeFight;
        float SurvivalMult    = TTK_B / Mathf.Max(TTK_A, 0.01f);

        _CalculatedPower  = V_net * SurvivalMult;
        _ValueDiscrepancy = _CalculatedPower - _spawnCost;

        _Calc_PushingPower = (TotalKB_A * D_gainA) / TimeFight;
        _Calc_DPS          = A_Atk * FreqA;
        _Calc_Defense      = TTK_B;
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