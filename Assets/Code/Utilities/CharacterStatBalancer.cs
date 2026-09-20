using UnityEngine;

public class CharacterStatBalancer : MonoBehaviour
{
    [Header("Stats Reference")]
    public ScriptableStats Stats;

    [Header("Master Balancing Reference")]
    public MasterBalancingScript MasterBalancing;

    [Header("--- ANALYSIS (Live Data) ---")]
    public MarginalUtility Marginal_Utility; // declared but never populated in the original scripts either — left as-is, not wired up
    public CharacterCurves Power_Curves;
    public DetailedCalculations Comparison_Metrics;

    void Update()
    {
        if (Stats == null || MasterBalancing == null) return;

        var grapher = MasterBalancing.GetComponent<BalancingGrapher>();
        if (grapher == null || grapher.AnchorUnit == null) return;

        var anchor = PowerMath.GetProfile(grapher.AnchorUnit);
        RefreshBalancing(grapher, anchor, MasterBalancing.MinPowerOffset);
    }

    public void RefreshBalancing(BalancingGrapher g, PowerMath.UnitProfile anchor, float powerOffset)
    {
        if (Stats == null) return;

        var result = CalculatePower(Stats, anchor,
            g.Weight_AttackDamage, g.Weight_AttackEndlag, g.Weight_MoveSpeed, g.Weight_KnockBackDamage,
            g.Weight_MaxHealth, g.Weight_KnockBackHealth, g.Weight_HorizontalRange, g.Weight_AOE,
            g.Base_Velocity, g.SimulationDistance, powerOffset);

        Stats._CalculatedPower = result.Power;
        Stats._ValueDiscrepancy = result.Power - Stats._spawnCost;

        Comparison_Metrics.My_TTK_Seconds = result.TTK_EnemyKillsMe;
        Comparison_Metrics.TimeToDie_Vs_Avg = $"{result.TTK_EnemyKillsMe:F2}s (I kill anchor in: {result.TTK_IKillEnemy:F2}s)";
        Comparison_Metrics.CombatPower = result.CombatPower;
        Comparison_Metrics.UtilityPower = result.UtilityPower;

        var self = PowerMath.GetProfile(Stats);
        float wKBH = g.Weight_KnockBackHealth;
        float wKBD = g.Weight_KnockBackDamage;
        float myHits = (self.KBH * wKBH) / Mathf.Max(anchor.KBD * wKBD, 1f);
        float anchorHits = (anchor.KBH * wKBH) / Mathf.Max(anchor.KBD * wKBD, 1f);
        Comparison_Metrics.My_HitsToKB = myHits;
        Comparison_Metrics.HitsToKB_Vs_Avg = $"{myHits:F1} hits (Anchor: {anchorHits:F1} hits)";

        UpdateCurves(g, self, anchor);
    }

    private void UpdateCurves(BalancingGrapher g, PowerMath.UnitProfile self, PowerMath.UnitProfile anchor)
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
            Power_Curves.MoveSpeed.AddKey(t * g.Max_MoveSpeed, SweepPower(self, anchor, g, p => { p.Move = t * g.Max_MoveSpeed; return p; }));
            Power_Curves.AttackEndlag.AddKey(t * g.Max_Endlag, SweepPower(self, anchor, g, p => { p.Cooldown = Mathf.Max(t * g.Max_Endlag, 0.01f); return p; }));
            Power_Curves.HorizontalRange.AddKey(t * g.Max_Range, SweepPower(self, anchor, g, p => { p.Range = t * g.Max_Range; return p; }));
            Power_Curves.MaxHealth.AddKey(t * g.Max_Health, SweepPower(self, anchor, g, p => { p.HP = t * g.Max_Health; return p; }));
            Power_Curves.AttackDamage.AddKey(t * g.Max_Damage, SweepPower(self, anchor, g, p => { p.Atk = t * g.Max_Damage; return p; }));
            Power_Curves.KnockBackDamage.AddKey(t * g.Max_KBDamage, SweepPower(self, anchor, g, p => { p.KBD = t * g.Max_KBDamage; return p; }));
            Power_Curves.KnockBackMaxHealth.AddKey(t * g.Max_KBHealth, SweepPower(self, anchor, g, p => { p.KBH = t * g.Max_KBHealth; return p; }));
        }
    }

    private float SweepPower(PowerMath.UnitProfile baseProfile, PowerMath.UnitProfile anchor, BalancingGrapher g,
        System.Func<PowerMath.UnitProfile, PowerMath.UnitProfile> edit)
    {
        var p = edit(baseProfile);
        var result = CalculatePowerRaw(p, anchor,
            g.Weight_AttackDamage, g.Weight_AttackEndlag, g.Weight_MoveSpeed, g.Weight_KnockBackDamage,
            g.Weight_MaxHealth, g.Weight_KnockBackHealth, g.Weight_HorizontalRange, g.Weight_AOE,
            g.Base_Velocity, g.SimulationDistance);
        return result.CombatPower;
    }

    // Combat power (duel sim, vs the fixed anchor) + utility power (heals/status,
    // computed straight from the ScriptableStats' non-offensive actions) + offset.
    public static PowerResult CalculatePower(
        ScriptableStats s, PowerMath.UnitProfile anchor,
        float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float wAOE,
        float baseVelocity, float simDist, float powerOffset)
    {
        if (s == null) return default;

        var result = CalculatePowerRaw(PowerMath.GetProfile(s), anchor,
            wAtk, wEnd, wMove, wKB_Dmg, wHP, wKB_HP, wRange, wAOE, baseVelocity, simDist);

        result.UtilityPower = PowerMath.GetUtilityPower(s);
        result.Power = result.CombatPower + result.UtilityPower + powerOffset;
        return result;
    }

    // The ONE implementation of the duel simulation (previously existed twice
    // as near-identical copies). Returns combat power only — utility power and
    // the offset are added on top by CalculatePower.
    public static PowerResult CalculatePowerRaw(
        PowerMath.UnitProfile self, PowerMath.UnitProfile anchor,
        float wAtk, float wEnd, float wMove, float wKB_Dmg, float wHP, float wKB_HP, float wRange, float wAOE,
        float baseVelocity, float simDist)
    {
        // Modest, diminishing-returns bonus for hitting multiple targets — priced
        // for typical mixed combat, not a swarm's full clear potential. See chat
        // notes on why AOE shouldn't be priced for its best-case matchup.
        float A_AoeMult = 1f + (Mathf.Min(self.MaxTargets, 20) - 1) * wAOE;
        float B_AoeMult = 1f + (Mathf.Min(anchor.MaxTargets, 20) - 1) * wAOE;

        float A_HP    = Mathf.Max(self.HP * wHP, 0.1f);
        float A_KBH   = Mathf.Max(self.KBH * wKB_HP, 0.1f);
        float A_Move  = Mathf.Max(self.Move * wMove, 0.1f);
        float A_KBD   = self.KBD * wKB_Dmg;
        float A_Atk   = self.Atk * wAtk * Mathf.Max(A_AoeMult, 0.01f);
        float A_End   = Mathf.Max(self.Cooldown * wEnd, 0.01f);
        float A_Range = self.Range * wRange;

        float B_HP    = Mathf.Max(anchor.HP * wHP, 0.1f);
        float B_KBH   = Mathf.Max(anchor.KBH * wKB_HP, 0.1f);
        float B_Move  = Mathf.Max(anchor.Move * wMove, 0.1f);
        float B_KBD   = anchor.KBD * wKB_Dmg;
        float B_Atk   = anchor.Atk * wAtk * Mathf.Max(B_AoeMult, 0.01f);
        float B_End   = Mathf.Max(anchor.Cooldown * wEnd, 0.01f);
        float B_Range = anchor.Range * wRange;

        float FreqA = 1f / (A_End + 0.5f);
        float FreqB = 1f / (B_End + 0.5f);

        float tApp = Mathf.Max(((simDist / 2f) - (A_Range + B_Range) / 2f) / (A_Move + B_Move), 0f);

        float ttkA = (B_Atk * FreqB) > 0 ? A_HP / (B_Atk * FreqB) : 1000f; // seconds for the anchor to kill me
        float ttkB = (A_Atk * FreqA) > 0 ? B_HP / (A_Atk * FreqA) : 1000f; // seconds for me to kill the anchor

        float vPush = ((A_KBD / Mathf.Max(B_KBH, 1f)) * baseVelocity * FreqA) -
                      ((B_KBD / Mathf.Max(A_KBH, 1f)) * baseVelocity * FreqB);

        float tPush = vPush != 0 ? (simDist / 2f) / Mathf.Abs(vPush) : 1000f;
        float tFight = Mathf.Min(ttkA, ttkB, tPush);
        float xEnd = vPush * tFight;

        int winSide = (ttkB < ttkA || (vPush > 0 && tFight == tPush)) ? 1 : -1;
        float winnerMove = winSide > 0 ? A_Move : B_Move;
        float tWalk = ((simDist / 2f) - (winSide * xEnd)) / Mathf.Max(winnerMove, 0.1f);

        float combatPower = winSide * (simDist / Mathf.Max(tApp + tFight + tWalk, 0.1f));

        return new PowerResult
        {
            Power = combatPower,
            CombatPower = combatPower,
            UtilityPower = 0f,
            TTK_EnemyKillsMe = ttkA,
            TTK_IKillEnemy = ttkB,
            WinSide = winSide
        };
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
    public float CombatPower;
    public float UtilityPower;
}

[System.Serializable]
public struct PowerResult
{
    public float Power;        // CombatPower + UtilityPower + offset
    public float CombatPower;
    public float UtilityPower;
    public float TTK_EnemyKillsMe;
    public float TTK_IKillEnemy;
    public int WinSide;
}
#endregion