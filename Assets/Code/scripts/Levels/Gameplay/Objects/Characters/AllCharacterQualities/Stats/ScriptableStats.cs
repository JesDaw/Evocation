using System.Collections.Generic;
using UnityEngine;

// 1. DATA STRUCTURES FOR DROPDOWNS
[System.Serializable]
public struct StatDelta 
{ 
    public float PlusOne; 
    public float MinusOne; 
}

[System.Serializable]
public class MarginalUtility
{
    public StatDelta AttackDamage;
    public StatDelta AttackEndlag;
    public StatDelta MoveSpeed;
    public StatDelta KnockBackDamage;
    public StatDelta MaxHealth;
    public StatDelta KB_MaxHealth;
    public StatDelta Range;
}

[System.Serializable]
public class CharacterCurves
{
    public AnimationCurve AttackDamage = new();
    public AnimationCurve AttackEndlag = new();
    public AnimationCurve MoveSpeed = new();
    public AnimationCurve KnockBackDamage = new();
    public AnimationCurve MaxHealth = new();
    public AnimationCurve KnockBackMaxHealth = new();
    public AnimationCurve HorizontalRange = new();
}

[System.Serializable]
public class DetailedCalculations
{
    public float PushingPower;
    public float DPS;
    public float Defense_TTK;
}

// 2. MAIN SCRIPTABLE OBJECT
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
    [Range(0, 100)] public float _MoveSpeed;
    [Range(0, 100)] public float _KnockBackDamage;

    [Header("Damage Per Second")]
    [Range(0, 100)] public int _AttackDamage;
    [Range(0, 100)] public float _AttackEndlag;

    [Header("Defense")]
    public int _MaxHealth = 1;
    public float _KnockBackMaxHealth = 1;
    [Range(0, 100)] public float _VerticalRange = 2f;
    [Range(0, 100)] public float _HorizontalRange = 2f;

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

    [Header("--- ANALYSIS (Live Data) ---")]
    [Tooltip("How much power changes if you add/subtract 1 from current stats")]
    public MarginalUtility Marginal_Utility;

    [Tooltip("Visual graphs of stat scaling for THIS character specifically")]
    public CharacterCurves Power_Curves;

    [Tooltip("Raw simulation numbers for internal logic check")]
    public DetailedCalculations Calculated_Totals;

    // --- REFRESH LOGIC (Called by Master Script) ---
    public void RefreshBalancing(
        float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float wAOE,
        float avgHP, float avgKB_HP, float avgMove, float avgKB_Dmg,
        float avgAtk, float avgEndlag, float avgRange,
        float baseVelocity, float baseAngle,
        float universalSimDist, float universalMaxStat)
    {
        // 1. Calculate Base Power
        _CalculatedPower = SimulatePower(
            _AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange,
            wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange,
            avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist);
            
        _ValueDiscrepancy = _CalculatedPower - _spawnCost;

        // 2. Calculate Marginal Utility (+/- 1)
        Marginal_Utility.AttackDamage.PlusOne = SimulatePower(_AttackDamage + 1, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;
        Marginal_Utility.AttackDamage.MinusOne = SimulatePower(Mathf.Max(0, _AttackDamage - 1), _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;

        Marginal_Utility.AttackEndlag.PlusOne = SimulatePower(_AttackDamage, _AttackEndlag + 1, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;
        Marginal_Utility.AttackEndlag.MinusOne = SimulatePower(_AttackDamage, Mathf.Max(0, _AttackEndlag - 1), _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;

        Marginal_Utility.MoveSpeed.PlusOne = SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed + 1, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;
        Marginal_Utility.MoveSpeed.MinusOne = SimulatePower(_AttackDamage, _AttackEndlag, Mathf.Max(0, _MoveSpeed - 1), _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;

        Marginal_Utility.KnockBackDamage.PlusOne = SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage + 1, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;
        Marginal_Utility.KnockBackDamage.MinusOne = SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, Mathf.Max(0, _KnockBackDamage - 1), _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;

        Marginal_Utility.MaxHealth.PlusOne = SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth + 1, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;
        Marginal_Utility.MaxHealth.MinusOne = SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, Mathf.Max(1, _MaxHealth - 1), _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;

        Marginal_Utility.KB_MaxHealth.PlusOne = SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth + 1, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;
        Marginal_Utility.KB_MaxHealth.MinusOne = SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, Mathf.Max(1, _KnockBackMaxHealth - 1), _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;

        Marginal_Utility.Range.PlusOne = SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange + 1, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;
        Marginal_Utility.Range.MinusOne = SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, Mathf.Max(0, _HorizontalRange - 1), wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist) - _CalculatedPower;

        // 3. Fill Character Specific Curves
        UpdateCurves(wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, universalSimDist, universalMaxStat);

        // 4. Detailed Totals
        float FreqA = 1f / (Mathf.Max(_AttackEndlag * wEnd, 0.01f) + 0.5f);
        float FreqB = 1f / (Mathf.Max(avgEndlag, 0.01f) + 0.5f);
        Calculated_Totals.DPS = _AttackDamage * wAtk * FreqA;
        Calculated_Totals.Defense_TTK = (_MaxHealth * wHP) / Mathf.Max(avgAtk * FreqB, 0.01f);
        Calculated_Totals.PushingPower = ((_KnockBackDamage * wKB_Dmg / Mathf.Max(avgKB_HP, 1f)) * baseVelocity * FreqA) - 
                                         ((avgKB_Dmg / Mathf.Max(_KnockBackMaxHealth * wKB_HP, 1f)) * baseVelocity * FreqB);
    }

    private void UpdateCurves(float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float avgHP, float avgKB_HP, float avgMove, float avgKB_Dmg, float avgAtk, float avgEndlag, float avgRange, float baseVelocity, float simDist, float maxStat)
    {
        Power_Curves.AttackDamage = new AnimationCurve();
        Power_Curves.AttackEndlag = new AnimationCurve();
        Power_Curves.MoveSpeed = new AnimationCurve();
        Power_Curves.KnockBackDamage = new AnimationCurve();
        Power_Curves.MaxHealth = new AnimationCurve();
        Power_Curves.KnockBackMaxHealth = new AnimationCurve();
        Power_Curves.HorizontalRange = new AnimationCurve();

        int res = 20; // Lower resolution for ScriptableObject performance
        for (int i = 0; i <= res; i++)
        {
            float x = (i / (float)res) * maxStat;
            Power_Curves.AttackDamage.AddKey(x, SimulatePower(x, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.AttackEndlag.AddKey(x, SimulatePower(_AttackDamage, x, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.MoveSpeed.AddKey(x, SimulatePower(_AttackDamage, _AttackEndlag, x, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.KnockBackDamage.AddKey(x, SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, x, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.MaxHealth.AddKey(x, SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, x, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.KnockBackMaxHealth.AddKey(x, SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, x, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.HorizontalRange.AddKey(x, SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, x, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
        }
    }

    private float SimulatePower(float myAtk, float myEnd, float myMove, float myKBD, float myHP, float myKBH, float myRange,
        float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange,
        float avgHP, float avgKB_HP, float avgMove, float avgKB_Dmg, float avgAtk, float avgEndlag, float avgRange,
        float baseVelocity, float simDist)
    {
        float A_HP = Mathf.Max(myHP * wHP, 0.1f);
        float A_KBH = Mathf.Max(myKBH * wKB_HP, 0.1f);
        float A_Move = Mathf.Max(myMove * wMove, 0.1f);
        float A_KBD = myKBD * wKB_Dmg;
        float A_Atk = myAtk * wAtk;
        float A_End = Mathf.Max(myEnd * wEnd, 0.01f);
        float A_Range = myRange * wRange;

        float FreqA = 1f / (A_End + 0.5f);
        float FreqB = 1f / (Mathf.Max(avgEndlag, 0.01f) + 0.5f);
        
        float tApp = Mathf.Max(((simDist / 2f) - (A_Range + avgRange) / 2f) / (A_Move + avgMove), 0f);
        float ttkA = (avgAtk * FreqB) > 0 ? A_HP / (avgAtk * FreqB) : 1000f;
        float ttkB = (A_Atk * FreqA) > 0 ? avgHP / (A_Atk * FreqA) : 1000f;

        float vPush = ((A_KBD / Mathf.Max(avgKB_HP, 1f)) * baseVelocity * FreqA) - 
                      ((avgKB_Dmg / Mathf.Max(A_KBH, 1f)) * baseVelocity * FreqB);

        float tPush = vPush != 0 ? (simDist / 2f) / Mathf.Abs(vPush) : 1000f;
        float tFight = Mathf.Min(ttkA, ttkB, tPush);
        float xEnd = vPush * tFight;

        float WinSide = (ttkB < ttkA || (vPush > 0 && tFight == tPush)) ? 1f : -1f;
        float winnerMove = WinSide > 0 ? A_Move : avgMove;
        float tWalk = ((simDist / 2f) - (WinSide * xEnd)) / winnerMove;
        
        return WinSide * (simDist / Mathf.Max(tApp + tFight + tWalk, 0.1f));
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