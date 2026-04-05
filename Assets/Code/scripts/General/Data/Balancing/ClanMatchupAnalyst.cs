using UnityEngine;

[ExecuteInEditMode]
public class ClanMatchupAnalyst : MonoBehaviour
{
    public ClanStats ClanA;
    public ClanStats ClanB;

    [Header("Results: Clan A vs Clan B")]
    public float ClanA_Advantage;
    [TextArea(3, 5)] public string Analysis_A;

    [Header("Results: Clan B vs Clan A")]
    public float ClanB_Advantage;
    [TextArea(3, 5)] public string Analysis_B;

    [Header("Global Correction")]
    public float LocalMinOffset; // The shift applied to these specific results

    [SerializeField] BalancingGrapher grapher;

    void Update()
    {
        if (ClanA == null || ClanB == null) return;
        if (grapher == null) grapher = GetComponent<BalancingGrapher>();

        // 1. Get Clan B's Averages to test Clan A against
        float bHP = ClanB.AvgHP;
        float bKBH = ClanB.AvgKB_HP;
        float bMove = ClanB.AvgMove;
        float bKBD = ClanB.AvgKB_Dmg;
        float bAtk = ClanB.AvgAtk_Dmg;
        float bEnd = ClanB.AvgEndlag;
        float bRng = ClanB.AvgRange;

        // 2. Get Clan A's Averages to test Clan B against
        float aHP = ClanA.AvgHP;
        float aKBH = ClanA.AvgKB_HP;
        float aMove = ClanA.AvgMove;
        float aKBD = ClanA.AvgKB_Dmg;
        float aAtk = ClanA.AvgAtk_Dmg;
        float aEnd = ClanA.AvgEndlag;
        float aRng = ClanA.AvgRange;

        float minPowerFound = 0;

        // 3. Simulate Clan A units against Clan B averages (Pass 1: Find Min)
        float rawPowerA = 0;
        foreach(var s in ClanA.all_stats_scripts)
        {
            if (s == null) continue;
            float p = CharacterStatBalancer.CalculatePowerRaw(
                s._AttackDamage, s._ExtraEndlag, s._MoveSpeed, s._KnockBackDamage, s._MaxHealth, s._KnockBackMaxHealth, s._HorizontalRange,
                grapher.Weight_AttackDamage, grapher.Weight_AttackEndlag, grapher.Weight_MoveSpeed, grapher.Weight_KnockBackDamage, 
                grapher.Weight_MaxHealth, grapher.Weight_KnockBackHealth, grapher.Weight_HorizontalRange,
                bHP, bKBH, bMove, bKBD, bAtk, bEnd, bRng,
                grapher.Base_Velocity, grapher.SimulationDistance
            );
            rawPowerA += p;
            if (p < minPowerFound) minPowerFound = p;
        }

        // 4. Simulate Clan B units against Clan A averages (Pass 1: Find Min)
        float rawPowerB = 0;
        foreach(var s in ClanB.all_stats_scripts)
        {
            if (s == null) continue;
            float p = CharacterStatBalancer.CalculatePowerRaw(
                s._AttackDamage, s._ExtraEndlag, s._MoveSpeed, s._KnockBackDamage, s._MaxHealth, s._KnockBackMaxHealth, s._HorizontalRange,
                grapher.Weight_AttackDamage, grapher.Weight_AttackEndlag, grapher.Weight_MoveSpeed, grapher.Weight_KnockBackDamage, 
                grapher.Weight_MaxHealth, grapher.Weight_KnockBackHealth, grapher.Weight_HorizontalRange,
                aHP, aKBH, aMove, aKBD, aAtk, aEnd, aRng,
                grapher.Base_Velocity, grapher.SimulationDistance
            );
            rawPowerB += p;
            if (p < minPowerFound) minPowerFound = p;
        }

        // Calculate the offset for this specific matchup
        LocalMinOffset = Mathf.Abs(minPowerFound) + 1f;

        // 5. Calculate Final Averages with the offset applied
        // We add the offset to the total sum * unit count to apply it to every unit
        float countA = ClanA.all_stats_scripts.Length;
        float countB = ClanB.all_stats_scripts.Length;

        ClanA_Advantage = (rawPowerA + (LocalMinOffset * countA)) / countA;
        ClanB_Advantage = (rawPowerB + (LocalMinOffset * countB)) / countB;

        Analysis_A = $"{ClanA.ClanTheme} vs {ClanB.ClanTheme} local power: {ClanA_Advantage:F2} (Offset: +{LocalMinOffset:F1})";
        Analysis_B = $"{ClanB.ClanTheme} vs {ClanA.ClanTheme} local power: {ClanB_Advantage:F2} (Offset: +{LocalMinOffset:F1})";
    }
}