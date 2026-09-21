using UnityEngine;

// Intentionally different from the anchor-based cost system: this compares
// ClanA's actual roster against ClanB's actual roster (and vice versa) for
// matchup/RPS exploration, not for pricing. Using each clan's own live
// average here is correct for "does clan A beat clan B on average" — it's
// only the cost-calibration system (MasterBalancingScript/ClanStats) that
// needed a fixed reference instead of a moving one. Flag if you'd rather
// this also use a fixed anchor for some other reason.
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

    [Header("Calibration")]
    [Tooltip("Right-click this component and choose 'Recalibrate Local Offset' after changing either " +
             "clan's roster — no longer recomputed every frame, same reasoning as MasterBalancingScript.")]
    public float LocalMinOffset;

    [SerializeField] BalancingGrapher grapher;

    void Update()
    {
        if (ClanA == null || ClanB == null) return;
        if (grapher == null) grapher = GetComponent<BalancingGrapher>();
        if (grapher == null) return;

        var avgB = ProfileOf(ClanB);
        var avgA = ProfileOf(ClanA);

        float sumA = SumPower(ClanA, avgB);
        float sumB = SumPower(ClanB, avgA);

        float countA = ClanA.all_stats_scripts.Length;
        float countB = ClanB.all_stats_scripts.Length;

        ClanA_Advantage = (sumA + (LocalMinOffset * countA)) / countA;
        ClanB_Advantage = (sumB + (LocalMinOffset * countB)) / countB;

        Analysis_A = $"{ClanA.ClanTheme} vs {ClanB.ClanTheme} local power: {ClanA_Advantage:F2} (Offset: +{LocalMinOffset:F1})";
        Analysis_B = $"{ClanB.ClanTheme} vs {ClanA.ClanTheme} local power: {ClanB_Advantage:F2} (Offset: +{LocalMinOffset:F1})";
    }

    PowerMath.UnitProfile ProfileOf(ClanStats c) => new PowerMath.UnitProfile
    {
        Atk = c.AvgAtk_Dmg, Move = c.AvgMove, KBD = c.AvgKB_Dmg,
        HP = c.AvgHP, KBH = c.AvgKB_HP, Range = c.AvgRange, Cooldown = c.AvgCooldown,
        MaxTargets = 1 // a clan average doesn't have a meaningful "average AOE cap" — treated as single-target
    };

    float SumPower(ClanStats clan, PowerMath.UnitProfile opponentAvg)
    {
        float sum = 0f;
        foreach (var cd in clan.all_stats_scripts)
        {
            if (cd?.scriptableStats == null) continue;
            var result = CharacterStatBalancer.CalculatePower(cd.scriptableStats, opponentAvg,
                grapher.Weight_AttackDamage, grapher.Weight_AttackEndlag, grapher.Weight_MoveSpeed, grapher.Weight_KnockBackDamage,
                grapher.Weight_MaxHealth, grapher.Weight_KnockBackHealth, grapher.Weight_HorizontalRange, grapher.Weight_AOE,
                grapher.Base_Velocity, grapher.SimulationDistance, 0f);
            sum += result.Power;
        }
        return sum;
    }

    [ContextMenu("Recalibrate Local Offset")]
    public void RecalibrateOffset()
    {
        if (ClanA == null || ClanB == null) return;
        if (grapher == null) grapher = GetComponent<BalancingGrapher>();
        if (grapher == null) return;

        var avgB = ProfileOf(ClanB);
        var avgA = ProfileOf(ClanA);

        float minFound = 0f;

        void CheckAll(ClanStats clan, PowerMath.UnitProfile opponentAvg)
        {
            foreach (var cd in clan.all_stats_scripts)
            {
                if (cd?.scriptableStats == null) continue;
                var result = CharacterStatBalancer.CalculatePower(cd.scriptableStats, opponentAvg,
                    grapher.Weight_AttackDamage, grapher.Weight_AttackEndlag, grapher.Weight_MoveSpeed, grapher.Weight_KnockBackDamage,
                    grapher.Weight_MaxHealth, grapher.Weight_KnockBackHealth, grapher.Weight_HorizontalRange, grapher.Weight_AOE,
                    grapher.Base_Velocity, grapher.SimulationDistance, 0f);
                if (result.Power < minFound) minFound = result.Power;
            }
        }
        CheckAll(ClanA, avgB);
        CheckAll(ClanB, avgA);

        LocalMinOffset = Mathf.Abs(minFound) + 1f;
        Debug.Log($"[ClanMatchupAnalyst] Recalibrated LocalMinOffset = {LocalMinOffset:F2}");
    }
}