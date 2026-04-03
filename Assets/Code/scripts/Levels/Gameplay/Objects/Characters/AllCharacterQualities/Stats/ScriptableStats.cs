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

    [Header("--- ANALYSIS (Live Data) ---")]
    public MarginalUtility Marginal_Utility;
    public CharacterCurves Power_Curves;
    public DetailedCalculations Comparison_Metrics;

    #region balancing stuff

    public void RefreshBalancing(
        float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float wAOE,
        float avgHP, float avgKB_HP, float avgMove, float avgKB_Dmg,
        float avgAtk, float avgEndlag, float avgRange,
        float baseVelocity, float baseAngle,
        float simDist,
        float mMove, float mEnd, float mRange, float mHP, float mAtk, float mKBD, float mKBH,
        float powerOffset)
    {
        float rawPower = SimulatePower(
            _AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange,
            wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange,
            avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist);
            
        _CalculatedPower = rawPower + powerOffset;
        _ValueDiscrepancy = _CalculatedPower - _spawnCost;

        // Metrics Comparison Logic
        float myFreq = 1f / (Mathf.Max(_AttackEndlag * wEnd, 0.01f) + 0.5f);
        float avgFreq = 1f / (Mathf.Max(avgEndlag, 0.01f) + 0.5f);
        float avgEnemyDPS = (avgAtk * wAtk) * avgFreq;

        float myTTK = (_MaxHealth * wHP) / Mathf.Max(avgEnemyDPS, 0.1f);
        float globalAvgTTK = (avgHP * wHP) / Mathf.Max(avgEnemyDPS, 0.1f);
        Comparison_Metrics.My_TTK_Seconds = myTTK;
        Comparison_Metrics.TimeToDie_Vs_Avg = $"{myTTK:F2}s (Avg: {globalAvgTTK:F2}s)";

        float myHits = (_KnockBackMaxHealth * wKB_HP) / Mathf.Max(avgKB_Dmg * wKB_Dmg, 1f);
        float globalAvgHits = (avgKB_HP * wKB_HP) / Mathf.Max(avgKB_Dmg * wKB_Dmg, 1f);
        Comparison_Metrics.My_HitsToKB = myHits;
        Comparison_Metrics.HitsToKB_Vs_Avg = $"{myHits:F1} hits (Avg: {globalAvgHits:F1} hits)";

        UpdateCurves(wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist, mMove, mEnd, mRange, mHP, mAtk, mKBD, mKBH);
    }

    private void UpdateCurves(float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float avgHP, float avgKB_HP, float avgMove, float avgKB_Dmg, float avgAtk, float avgEndlag, float avgRange, float baseVelocity, float simDist, float mMove, float mEnd, float mRange, float mHP, float mAtk, float mKBD, float mKBH)
    {
        Power_Curves.MoveSpeed = new AnimationCurve();
        Power_Curves.AttackEndlag = new AnimationCurve();
        Power_Curves.HorizontalRange = new AnimationCurve();
        Power_Curves.MaxHealth = new AnimationCurve();
        Power_Curves.AttackDamage = new AnimationCurve();
        Power_Curves.KnockBackDamage = new AnimationCurve();
        Power_Curves.KnockBackMaxHealth = new AnimationCurve();

        int res = 15;
        for (int i = 0; i <= res; i++)
        {
            float t = i / (float)res;
            Power_Curves.MoveSpeed.AddKey(t * mMove, SimulatePower(_AttackDamage, _AttackEndlag, t * mMove, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.AttackEndlag.AddKey(t * mEnd, SimulatePower(_AttackDamage, t * mEnd, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.HorizontalRange.AddKey(t * mRange, SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, t * mRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.MaxHealth.AddKey(t * mHP, SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, t * mHP, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.AttackDamage.AddKey(t * mAtk, SimulatePower(t * mAtk, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.KnockBackDamage.AddKey(t * mKBD, SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, t * mKBD, _MaxHealth, _KnockBackMaxHealth, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.KnockBackMaxHealth.AddKey(t * mKBH, SimulatePower(_AttackDamage, _AttackEndlag, _MoveSpeed, _KnockBackDamage, _MaxHealth, t * mKBH, _HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
        }
    }

    public float SimulatePower(float myAtk, float myEnd, float myMove, float myKBD, float myHP, float myKBH, float myRange,
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
        float ttkA = (avgAtk * wAtk * FreqB) > 0 ? A_HP / (avgAtk * wAtk * FreqB) : 1000f;
        float ttkB = (A_Atk * FreqA) > 0 ? (avgHP * wHP) / (A_Atk * FreqA) : 1000f;

        float vPush = ((A_KBD / Mathf.Max(avgKB_HP * wKB_HP, 1f)) * baseVelocity * FreqA) - 
                      ((avgKB_Dmg * wKB_Dmg / Mathf.Max(A_KBH, 1f)) * baseVelocity * FreqB);

        float tPush = vPush != 0 ? (simDist / 2f) / Mathf.Abs(vPush) : 1000f;
        float tFight = Mathf.Min(ttkA, ttkB, tPush);
        float xEnd = vPush * tFight;

        float WinSide = (ttkB < ttkA || (vPush > 0 && tFight == tPush)) ? 1f : -1f;
        float winnerMove = WinSide > 0 ? A_Move : (avgMove * wMove);
        float tWalk = ((simDist / 2f) - (WinSide * xEnd)) / Mathf.Max(winnerMove, 0.1f);
        
        return WinSide * (simDist / Mathf.Max(tApp + tFight + tWalk, 0.1f));
    }
}

#endregion

public enum AttackStyle { Melee, Projectile }

#region System.Serializable stuff
[System.Serializable]
public struct StatDelta { public float PlusOne; public float MinusOne; }

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
    public string TimeToDie_Vs_Avg;
    public string HitsToKB_Vs_Avg;
    public float My_TTK_Seconds;
    public float My_HitsToKB;
}

[System.Serializable]
public class animationRigs
{
    public enum animationKey { Idle, Running, Attack, Knockback }
    public animationKey Key;
    public GameObject Rig;
    public Vector2 Offset;
}
#endregion