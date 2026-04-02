using UnityEngine;

[ExecuteInEditMode]
public class BalancingGrapher : MonoBehaviour
{
    [Header("Master Curve")]
    public AnimationCurve TotalPowerSum = new();

    [Header("Pillar Curves")]
    [Tooltip("KB Space Rate = spacePerKBCycle / kbCycleTime\n" +
             "spacePerKBCycle = MoveSpeed × (AirTime + ApproachTime)\n" +
             "kbCycleTime = (HitsToKB × Endlag) + AirTime + ApproachTime\n" +
             "ApproachTime = KBDistance / (MoveSpeed + AvgEnemyMoveSpeed)")]
    public AnimationCurve PushingPowerCurve = new();

    [Tooltip("DPS = (AttackDamage × wAtk × HitsPerSec × AOEMult) × RangeBonus\n" +
             "HitsPerSec = 1 / (Endlag × wEnd + 0.5)\n" +
             "RangeBonus = 1 + (Range × 0.1 × wRange)")]
    public AnimationCurve PressureCurve = new();

    [Tooltip("Defense = (MaxHealth × wHP) × KBResistance\n" +
             "KBResistance = 1 + (KBMaxHealth × 0.05 × wKB_HP)")]
    public AnimationCurve SustainabilityCurve = new();

    [Header("Individual Stat Curves (all other stats held at baseline)")]
    [Tooltip("Scales linearly through Pressure × Sustainability.")]
    public AnimationCurve AttackDamageCurve = new();

    [Tooltip("Hyperbolic — each extra second of endlag hurts less than the last.")]
    public AnimationCurve AttackEndlagCurve = new();

    [Tooltip("Linear through Momentum and post-kill walk speed.")]
    public AnimationCurve MoveSpeedCurve = new();

    [Tooltip("Quadratic — feeds into EffectiveVelocity which is squared in KBDistance.")]
    public AnimationCurve KnockBackDamageCurve = new();

    [Tooltip("Linear through Sustainability.")]
    public AnimationCurve MaxHealthCurve = new();

    [Tooltip("Multiplies Sustainability AND Momentum via KBResistance.")]
    public AnimationCurve KnockBackHealthCurve = new();

    [Tooltip("Multiplier on Pressure — needs AttackDamage to have impact.")]
    public AnimationCurve HorizontalRangeCurve = new();

    [Tooltip("Multiplier on Pressure — needs AttackDamage to have impact.")]
    public AnimationCurve AOEEfficiencyCurve = new();

    [Header("Weights")]
    public float Weight_MoveSpeed       = 1.0f;
    public float Weight_KnockBackDamage = 1.0f;
    public float Weight_AttackDamage    = 1.0f;
    public float Weight_AttackEndlag    = 1.0f;
    public float Weight_MaxHealth       = 1.0f;
    public float Weight_KnockBackHealth = 1.0f;
    public float Weight_HorizontalRange = 1.0f;
    public float Weight_AOE_Efficiency  = 1.0f;

    [Header("Graph Settings")]
    public int   Resolution   = 50;
    public float MaxStatValue = 100f;

    [Tooltip("All other stats are fixed at this value while one stat is varied. " +
             "Raise this to better see the effect of multiplier stats like Range and AOE.")]
    public float IndividualCurveBaseline = 25f;

    [Header("Physics Baselines")]
    public float Base_Velocity = 5f;
    public float Base_Angle    = 45f;

    // Called by MasterBalancingScript after global averages are computed.
    public void UpdateGraphs(float avgHP, float avgKBMaxHealth, float avgMoveSpeed, float spaceTarget)
    {
        TotalPowerSum       = new();
        PushingPowerCurve   = new();
        PressureCurve       = new();
        SustainabilityCurve = new();
        AttackDamageCurve   = new();
        AttackEndlagCurve   = new();
        MoveSpeedCurve      = new();
        KnockBackDamageCurve = new();
        MaxHealthCurve      = new();
        KnockBackHealthCurve = new();
        HorizontalRangeCurve = new();
        AOEEfficiencyCurve  = new();

        float g        = 9.81f;
        float angleRad = Base_Angle * Mathf.Deg2Rad;

        // Full simulation — mirrors ScriptableStats.RefreshBalancing exactly.
        // Returns power = spaceTarget / timeToClaimTarget.
        float Calc(float atkDmg, float endlag, float moveSpd, float kbDmg,
                   float hp,     float kbHp,   float range,   float aoe)
        {
            float endlagActual = Mathf.Max(endlag * Weight_AttackEndlag, 0.01f);
            float moveSpeed    = moveSpd * Weight_MoveSpeed;
            float hitsPerSec   = 1f / (endlagActual + 0.5f);

            float kbRatio    = (kbDmg * Weight_KnockBackDamage) / 100f;
            float vEff       = Base_Velocity * kbRatio;
            float airTime    = (2f * vEff * Mathf.Sin(angleRad)) / g;
            float kbDist     = (Mathf.Pow(vEff, 2f) * Mathf.Sin(2f * angleRad)) / g;

            float aoeMult    = 1f + (aoe  * Weight_AOE_Efficiency  * 0.1f);
            float rangeB     = 1f + (range * 0.1f * Weight_HorizontalRange);
            float kbRes      = 1f + (kbHp * 0.05f * Weight_KnockBackHealth);

            float effAtk     = atkDmg * Weight_AttackDamage * aoeMult * rangeB;
            float effKB      = kbDmg  * Weight_KnockBackDamage;

            float hitsToKill = (effAtk > 0f) ? (avgHP         / effAtk)                    : float.MaxValue;
            float hitsToKB   = (effKB  > 0f) ? Mathf.Max(avgKBMaxHealth / effKB, 1f) : float.MaxValue;

            float killTime     = (hitsToKill < float.MaxValue) ? hitsToKill * endlagActual : float.MaxValue;
            float kbAtkTime    = (hitsToKB   < float.MaxValue) ? hitsToKB   * endlagActual : float.MaxValue;

            float combinedSpd  = moveSpeed + avgMoveSpeed;
            float approachTime = (combinedSpd > 0f && kbDist > 0f) ? kbDist / combinedSpd : 0f;

            float cycleTime    = (kbAtkTime < float.MaxValue) ? kbAtkTime + airTime + approachTime : float.MaxValue;
            float spacePerCycle = moveSpeed * (airTime + approachTime);

            float numCycles    = (cycleTime < float.MaxValue && killTime < float.MaxValue && cycleTime > 0f)
                ? killTime / cycleTime : 0f;
            float spaceDuringKill = numCycles * spacePerCycle;

            bool canKill = effAtk > 0f;
            bool canKB   = effKB  > 0f && spacePerCycle > 0f && cycleTime < float.MaxValue;

            float remaining  = spaceTarget - spaceDuringKill;
            float timeToTarget;

            if (!canKill && !canKB)
                timeToTarget = float.MaxValue;
            else if (!canKill)
            {
                float rate = (cycleTime > 0f) ? spacePerCycle / cycleTime : 0f;
                timeToTarget = (rate > 0f) ? spaceTarget / rate : float.MaxValue;
            }
            else if (remaining <= 0f)
                timeToTarget = killTime;
            else if (moveSpeed > 0f)
                timeToTarget = killTime + remaining / moveSpeed;
            else
                timeToTarget = float.MaxValue;

            return (timeToTarget > 0f && timeToTarget < float.MaxValue)
                ? spaceTarget / timeToTarget : 0f;
        }

        float b = IndividualCurveBaseline;

        for (int i = 0; i <= Resolution; i++)
        {
            float x = (i / (float)Resolution) * MaxStatValue;

            // Master curve — all stats scale together
            float total = Calc(x, x, x, x, x, x, x, x);
            TotalPowerSum.AddKey(x, total);

            // Pillar display curves at uniform x
            float endlagA  = Mathf.Max(x * Weight_AttackEndlag, 0.01f);
            float hps      = 1f / (endlagA + 0.5f);
            float aoeMAll  = 1f + (x * Weight_AOE_Efficiency  * 0.1f);
            float rangeBAll = 1f + (x * 0.1f * Weight_HorizontalRange);
            float kbResAll = 1f + (x * 0.05f * Weight_KnockBackHealth);
            float kbRAll   = (x * Weight_KnockBackDamage) / 100f;
            float vEffAll  = Base_Velocity * kbRAll;
            float airAll   = (2f * vEffAll * Mathf.Sin(angleRad)) / g;
            float kbDA     = (Mathf.Pow(vEffAll, 2f) * Mathf.Sin(2f * angleRad)) / g;
            float combAll  = x * Weight_MoveSpeed + avgMoveSpeed;
            float appAll   = (combAll > 0f && kbDA > 0f) ? kbDA / combAll : 0f;
            float cycleAll = (avgKBMaxHealth > 0f && x * Weight_KnockBackDamage > 0f)
                ? Mathf.Max(avgKBMaxHealth / (x * Weight_KnockBackDamage), 1f) * endlagA + airAll + appAll
                : float.MaxValue;
            float spcAll   = x * Weight_MoveSpeed * (airAll + appAll);

            PushingPowerCurve.AddKey(x,
                (cycleAll > 0f && cycleAll < float.MaxValue) ? spcAll / cycleAll : 0f);
            PressureCurve.AddKey(x,
                x * Weight_AttackDamage * hps * aoeMAll * rangeBAll);
            SustainabilityCurve.AddKey(x,
                (x * Weight_MaxHealth) * kbResAll);

            // Individual curves — one stat varies, rest at baseline
            AttackDamageCurve.AddKey(x,    Calc(x, b, b, b, b, b, b, b));
            AttackEndlagCurve.AddKey(x,    Calc(b, x, b, b, b, b, b, b));
            MoveSpeedCurve.AddKey(x,       Calc(b, b, x, b, b, b, b, b));
            KnockBackDamageCurve.AddKey(x, Calc(b, b, b, x, b, b, b, b));
            MaxHealthCurve.AddKey(x,       Calc(b, b, b, b, x, b, b, b));
            KnockBackHealthCurve.AddKey(x, Calc(b, b, b, b, b, x, b, b));
            HorizontalRangeCurve.AddKey(x, Calc(b, b, b, b, b, b, x, b));
            AOEEfficiencyCurve.AddKey(x,   Calc(b, b, b, b, b, b, b, x));
        }
    }
}