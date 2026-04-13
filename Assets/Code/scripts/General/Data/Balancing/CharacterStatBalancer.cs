using UnityEngine;

public class CharacterStatBalancer : MonoBehaviour
{
    [Header("Stats Reference")]
    public ScriptableStats Stats;

    [Header("Master Balancing Reference")]
    public MasterBalancingScript MasterBalancing;

    [Header("--- ANALYSIS (Live Data) ---")]
    public MarginalUtility Marginal_Utility;
    public CharacterCurves Power_Curves;
    public DetailedCalculations Comparison_Metrics;

    void Update()
    {
        if (Stats == null || MasterBalancing == null) return;

        var grapher = MasterBalancing.GetComponent<BalancingGrapher>();
        if (grapher == null) return;

        RefreshBalancing(
            grapher.Weight_AttackDamage, grapher.Weight_AttackEndlag, grapher.Weight_MoveSpeed, grapher.Weight_KnockBackDamage,
            grapher.Weight_MaxHealth, grapher.Weight_KnockBackHealth, grapher.Weight_HorizontalRange, 1f,
            MasterBalancing.Global_AvgHP, MasterBalancing.Global_AvgKB_MaxHealth, MasterBalancing.Global_AvgMoveSpeed, MasterBalancing.Global_AvgKB_Dmg,
            MasterBalancing.Global_AvgAtk_Dmg, MasterBalancing.Global_AvgEndlag, MasterBalancing.Global_AvgRange,
            grapher.Base_Velocity, 45f, grapher.SimulationDistance,
            grapher.Max_MoveSpeed, grapher.Max_Endlag, grapher.Max_Range, grapher.Max_Health, grapher.Max_Damage, grapher.Max_KBDamage, grapher.Max_KBHealth,
            MasterBalancing.MinPowerOffset
        );
    }

    public void RefreshBalancing(
        float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float wAOE,
        float avgHP, float avgKB_HP, float avgMove, float avgKB_Dmg,
        float avgAtk, float avgEndlag, float avgRange,
        float baseVelocity, float baseAngle,
        float simDist,
        float mMove, float mEnd, float mRange, float mHP, float mAtk, float mKBD, float mKBH,
        float powerOffset)
    {
        if (Stats == null) return;

        float rawPower = SimulatePower(
            Stats._AttackDamage, Stats._ExtraEndlag, Stats._MoveSpeed, Stats._KnockBackDamage, Stats._MaxHealth, Stats._KnockBackMaxHealth, Stats._HorizontalRange,
            wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange,
            avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist);
            
        Stats._CalculatedPower = rawPower + powerOffset;
        Stats._ValueDiscrepancy = Stats._CalculatedPower - Stats._spawnCost;

        float myFreq = 1f / (Mathf.Max(Stats._ExtraEndlag * wEnd, 0.01f) + 0.5f);
        float avgFreq = 1f / (Mathf.Max(avgEndlag, 0.01f) + 0.5f);
        float avgEnemyDPS = (avgAtk * wAtk) * avgFreq;

        float myTTK = (Stats._MaxHealth * wHP) / Mathf.Max(avgEnemyDPS, 0.1f);
        float globalAvgTTK = (avgHP * wHP) / Mathf.Max(avgEnemyDPS, 0.1f);
        Comparison_Metrics.My_TTK_Seconds = myTTK;
        Comparison_Metrics.TimeToDie_Vs_Avg = $"{myTTK:F2}s (Avg: {globalAvgTTK:F2}s)";

        float myHits = (Stats._KnockBackMaxHealth * wKB_HP) / Mathf.Max(avgKB_Dmg * wKB_Dmg, 1f);
        float globalAvgHits = (avgKB_HP * wKB_HP) / Mathf.Max(avgKB_Dmg * wKB_Dmg, 1f);
        Comparison_Metrics.My_HitsToKB = myHits;
        Comparison_Metrics.HitsToKB_Vs_Avg = $"{myHits:F1} hits (Avg: {globalAvgHits:F1} hits)";

        UpdateCurves(wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist, mMove, mEnd, mRange, mHP, mAtk, mKBD, mKBH);
    }

    private void UpdateCurves(float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float avgHP, float avgKB_HP, float avgMove, float avgKB_Dmg, float avgAtk, float avgEndlag, float avgRange, float baseVelocity, float simDist, float mMove, float mEnd, float mRange, float mHP, float mAtk, float mKBD, float mKBH)
    {
        if (Stats == null) return;

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
            Power_Curves.MoveSpeed.AddKey(t * mMove, SimulatePower(Stats._AttackDamage, Stats._ExtraEndlag, t * mMove, Stats._KnockBackDamage, Stats._MaxHealth, Stats._KnockBackMaxHealth, Stats._HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.AttackEndlag.AddKey(t * mEnd, SimulatePower(Stats._AttackDamage, t * mEnd, Stats._MoveSpeed, Stats._KnockBackDamage, Stats._MaxHealth, Stats._KnockBackMaxHealth, Stats._HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.HorizontalRange.AddKey(t * mRange, SimulatePower(Stats._AttackDamage, Stats._ExtraEndlag, Stats._MoveSpeed, Stats._KnockBackDamage, Stats._MaxHealth, Stats._KnockBackMaxHealth, t * mRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.MaxHealth.AddKey(t * mHP, SimulatePower(Stats._AttackDamage, Stats._ExtraEndlag, Stats._MoveSpeed, Stats._KnockBackDamage, t * mHP, Stats._KnockBackMaxHealth, Stats._HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.AttackDamage.AddKey(t * mAtk, SimulatePower(t * mAtk, Stats._ExtraEndlag, Stats._MoveSpeed, Stats._KnockBackDamage, Stats._MaxHealth, Stats._KnockBackMaxHealth, Stats._HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.KnockBackDamage.AddKey(t * mKBD, SimulatePower(Stats._AttackDamage, Stats._ExtraEndlag, Stats._MoveSpeed, t * mKBD, Stats._MaxHealth, Stats._KnockBackMaxHealth, Stats._HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
            Power_Curves.KnockBackMaxHealth.AddKey(t * mKBH, SimulatePower(Stats._AttackDamage, Stats._ExtraEndlag, Stats._MoveSpeed, Stats._KnockBackDamage, Stats._MaxHealth, t * mKBH, Stats._HorizontalRange, wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist));
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

    public static float CalculatePower(
        ScriptableStats s,
        float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange,
        float avgHP, float avgKB_HP, float avgMove, float avgKB_Dmg, float avgAtk, float avgEndlag, float avgRange,
        float baseVelocity, float simDist, float powerOffset)
    {
        if (s == null) return 0f;
        
        float rawPower = CalculatePowerRaw(
            s._AttackDamage, s._ExtraEndlag, s._MoveSpeed, s._KnockBackDamage, s._MaxHealth, s._KnockBackMaxHealth, s._HorizontalRange,
            wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange,
            avgHP, avgKB_HP, avgMove, avgKB_Dmg, avgAtk, avgEndlag, avgRange, baseVelocity, simDist);
            
        return rawPower + powerOffset;
    }

    public static float CalculatePowerRaw(float myAtk, float myEnd, float myMove, float myKBD, float myHP, float myKBH, float myRange,
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
#endregion
