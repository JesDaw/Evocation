using UnityEngine;

[ExecuteInEditMode]
public class BalancingGrapher : MonoBehaviour
{
    [Header("Master Curve")]
    public AnimationCurve TotalPowerSum = new();

    [Header("Pillar Curves")]
    [Tooltip("Gross KB velocity: (TotalKB_A × D_gainA) / TimeFight")]
    public AnimationCurve PushingPowerCurve = new();

    [Tooltip("Raw DPS = AttackDamage × wAtk × AOEMult × FreqA")]
    public AnimationCurve PressureCurve = new();

    [Tooltip("Defense = TTK_B = A_HP / avgDPS  (how long A survives)")]
    public AnimationCurve SustainabilityCurve = new();

    [Header("Individual Stat Curves")]
    [Tooltip("Y = total power with only AttackDamage varying. Others at global average.")]
    public AnimationCurve AttackDamageCurve    = new();
    public float          AttackDamage_MaxValue;
    public float          AttackDamage_Influence;

    [Tooltip("Y = total power with only AttackEndlag varying. Others at global average.")]
    public AnimationCurve AttackEndlagCurve    = new();
    public float          AttackEndlag_MaxValue;
    public float          AttackEndlag_Influence;

    [Tooltip("Y = total power with only MoveSpeed varying. Others at global average.")]
    public AnimationCurve MoveSpeedCurve       = new();
    public float          MoveSpeed_MaxValue;
    public float          MoveSpeed_Influence;

    [Tooltip("Y = total power with only KBDamage varying. Others at global average.")]
    public AnimationCurve KnockBackDamageCurve = new();
    public float          KnockBackDamage_MaxValue;
    public float          KnockBackDamage_Influence;

    [Tooltip("Y = total power with only MaxHealth varying. Others at global average.")]
    public AnimationCurve MaxHealthCurve       = new();
    public float          MaxHealth_MaxValue;
    public float          MaxHealth_Influence;

    [Tooltip("Y = total power with only KBMaxHealth varying. Others at global average.")]
    public AnimationCurve KnockBackHealthCurve = new();
    public float          KnockBackHealth_MaxValue;
    public float          KnockBackHealth_Influence;

    [Tooltip("Y = total power with only HorizontalRange varying. Others at global average.")]
    public AnimationCurve HorizontalRangeCurve = new();
    public float          HorizontalRange_MaxValue;
    public float          HorizontalRange_Influence;

    [Tooltip("Y = total power with only AOE varying. Others at global average.")]
    public AnimationCurve AOEEfficiencyCurve   = new();
    public float          AOEEfficiency_MaxValue;
    public float          AOEEfficiency_Influence;

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

    [Header("Physics Baselines (shared with simulation)")]
    public float Base_Velocity = 5f;
    public float Base_Angle    = 45f;

    public void UpdateGraphs(
        float avgHP, float avgKB_HP, float avgMove,
        float avgKB_Dmg, float avgAtk, float avgEndlag, float avgRange)
    {
        TotalPowerSum        = new();
        PushingPowerCurve    = new();
        PressureCurve        = new();
        SustainabilityCurve  = new();
        AttackDamageCurve    = new();
        AttackEndlagCurve    = new();
        MoveSpeedCurve       = new();
        KnockBackDamageCurve = new();
        MaxHealthCurve       = new();
        KnockBackHealthCurve = new();
        HorizontalRangeCurve = new();
        AOEEfficiencyCurve   = new();

        float g           = 9.81f;
        float avgAngleRad = Base_Angle * Mathf.Deg2Rad;
        float avgKBVel    = Base_Velocity;

        float Calc(float atkDmg, float endlag, float moveSpd, float kbDmg,
                   float hp,     float kbHp,   float range,   float aoe)
        {
            float A_Atk    = atkDmg  * Weight_AttackDamage * Mathf.Max(aoe * Weight_AOE_Efficiency, 1f);
            float A_Endlag = Mathf.Max(endlag * Weight_AttackEndlag, 0.01f);
            float A_Move   = moveSpd * Weight_MoveSpeed;
            float A_KB_Dmg = kbDmg   * Weight_KnockBackDamage;
            float A_HP     = hp      * Weight_MaxHealth;
            float A_KB_HP  = kbHp    * Weight_KnockBackHealth;
            float A_Range  = range   * Weight_HorizontalRange;

            float FreqA = 1f / (A_Endlag + 0.5f);
            float FreqB = 1f / (Mathf.Max(avgEndlag, 0.01f) + 0.5f);

            float TTK_A     = (A_Atk  * FreqA) > 0f ? avgHP / (A_Atk  * FreqA) : float.MaxValue;
            float TTK_B     = (avgAtk * FreqB) > 0f ? A_HP  / (avgAtk * FreqB) : float.MaxValue;
            float TimeFight = Mathf.Max(Mathf.Min(TTK_A, TTK_B), 0.01f);

            float FreeHitsA = (Mathf.Max(0f, A_Range - avgRange) / Mathf.Max(avgMove, 0.01f)) * FreqA;
            float FreeHitsB = (Mathf.Max(0f, avgRange - A_Range) / Mathf.Max(A_Move,  0.01f)) * FreqB;

            float kbRatioA  = A_KB_Dmg / Mathf.Max(avgKB_HP, 0.01f);
            float vEffA     = avgKBVel * kbRatioA;
            float D_KB_A    = (vEffA * vEffA * Mathf.Sin(2f * avgAngleRad)) / g;
            float AirTime_A = (2f * vEffA * Mathf.Sin(avgAngleRad)) / g;
            float t_meet_A  = (D_KB_A + (avgMove * AirTime_A) + (A_Move * A_Endlag))
                            / Mathf.Max(A_Move + avgMove, 0.01f);
            float D_gainA   = A_Move * Mathf.Max(t_meet_A - A_Endlag, 0f);

            float kbRatioB  = (avgKB_Dmg * Weight_KnockBackDamage) / Mathf.Max(A_KB_HP, 0.01f);
            float vEffB     = avgKBVel * kbRatioB;
            float D_KB_B    = (vEffB * vEffB * Mathf.Sin(2f * avgAngleRad)) / g;
            float AirTime_B = (2f * vEffB * Mathf.Sin(avgAngleRad)) / g;
            float t_meet_B  = (D_KB_B + (A_Move * AirTime_B) + (avgMove * avgEndlag))
                            / Mathf.Max(avgMove + A_Move, 0.01f);
            float D_gainB   = avgMove * Mathf.Max(t_meet_B - avgEndlag, 0f);

            float HTKB_A    = Mathf.Max(avgKB_HP / Mathf.Max(A_KB_Dmg,  0.01f), 1f);
            float HTKB_B    = Mathf.Max(A_KB_HP  / Mathf.Max(avgKB_Dmg, 0.01f), 1f);
            float TotalKB_A = ((TimeFight * FreqA) + FreeHitsA) / HTKB_A;
            float TotalKB_B = ((TimeFight * FreqB) + FreeHitsB) / HTKB_B;

            float NetDisp      = (TotalKB_A * D_gainA) - (TotalKB_B * D_gainB);
            float V_net        = NetDisp / TimeFight;
            float SurvivalMult = TTK_B   / Mathf.Max(TTK_A, 0.01f);

            return V_net * SurvivalMult;
        }

        // --- NEW BASELINES: Use the actual global averages ---
        float bAtk   = avgAtk;
        float bEnd   = avgEndlag;
        float bMove  = avgMove;
        float bKBD   = avgKB_Dmg;
        float bHP    = avgHP;
        float bKBH   = avgKB_HP;
        float bRange = avgRange;
        float bAOE   = 1f; // Baseline multiplier for AOE is 1 (no extra targets)

        // Calculate baseline power to determine precise influence (+1 to the stat)
        float basePower = Calc(bAtk, bEnd, bMove, bKBD, bHP, bKBH, bRange, bAOE);

        AttackDamage_Influence    = Calc(bAtk+1, bEnd, bMove, bKBD, bHP, bKBH, bRange, bAOE) - basePower;
        AttackEndlag_Influence    = Calc(bAtk, bEnd+1, bMove, bKBD, bHP, bKBH, bRange, bAOE) - basePower;
        MoveSpeed_Influence       = Calc(bAtk, bEnd, bMove+1, bKBD, bHP, bKBH, bRange, bAOE) - basePower;
        KnockBackDamage_Influence = Calc(bAtk, bEnd, bMove, bKBD+1, bHP, bKBH, bRange, bAOE) - basePower;
        MaxHealth_Influence       = Calc(bAtk, bEnd, bMove, bKBD, bHP+1, bKBH, bRange, bAOE) - basePower;
        KnockBackHealth_Influence = Calc(bAtk, bEnd, bMove, bKBD, bHP, bKBH+1, bRange, bAOE) - basePower;
        HorizontalRange_Influence = Calc(bAtk, bEnd, bMove, bKBD, bHP, bKBH, bRange+1, bAOE) - basePower;
        AOEEfficiency_Influence   = Calc(bAtk, bEnd, bMove, bKBD, bHP, bKBH, bRange, bAOE+1) - basePower;

        for (int i = 0; i <= Resolution; i++)
        {
            float x = (i / (float)Resolution) * MaxStatValue;

            // Master curve — all stats scale together
            TotalPowerSum.AddKey(x, Calc(x, x, x, x, x, x, x, x));

            // Pillar curves 
            float FreqX    = 1f / (Mathf.Max(x * Weight_AttackEndlag, 0.01f) + 0.5f);
            float kbRatioX = (x * Weight_KnockBackDamage) / Mathf.Max(avgKB_HP, 0.01f);
            float vEffX    = avgKBVel * kbRatioX;
            float D_KB_X   = (vEffX * vEffX * Mathf.Sin(2f * avgAngleRad)) / g;
            float airX     = (2f * vEffX * Mathf.Sin(avgAngleRad)) / g;
            float A_MoveX  = x * Weight_MoveSpeed;
            float A_EndX   = Mathf.Max(x * Weight_AttackEndlag, 0.01f);
            float t_meetX  = (D_KB_X + (avgMove * airX) + (A_MoveX * A_EndX))
                           / Mathf.Max(A_MoveX + avgMove, 0.01f);
            float D_gainX  = A_MoveX * Mathf.Max(t_meetX - A_EndX, 0f);
            float HTKB_X   = Mathf.Max(avgKB_HP / Mathf.Max(x * Weight_KnockBackDamage, 0.01f), 1f);
            float TTK_AX   = (x * Weight_AttackDamage * FreqX) > 0f
                           ? avgHP / (x * Weight_AttackDamage * FreqX) : float.MaxValue;
            float TTK_BX   = (avgAtk * (1f/(avgEndlag+0.5f))) > 0f
                           ? (x * Weight_MaxHealth) / (avgAtk * (1f/(avgEndlag+0.5f))) : float.MaxValue;
            float TimeX    = Mathf.Max(Mathf.Min(TTK_AX, TTK_BX), 0.01f);
            float TotalKBX = (TimeX * FreqX) / HTKB_X;

            PushingPowerCurve.AddKey(x,   (TotalKBX * D_gainX) / TimeX);
            PressureCurve.AddKey(x,        x * Weight_AttackDamage * FreqX);
            SustainabilityCurve.AddKey(x,  TTK_BX);

            // Individual curves — use global averages for the fixed variables
            AttackDamageCurve.AddKey(x,    Calc(x, bEnd, bMove, bKBD, bHP, bKBH, bRange, bAOE));
            AttackEndlagCurve.AddKey(x,    Calc(bAtk, x, bMove, bKBD, bHP, bKBH, bRange, bAOE));
            MoveSpeedCurve.AddKey(x,       Calc(bAtk, bEnd, x, bKBD, bHP, bKBH, bRange, bAOE));
            KnockBackDamageCurve.AddKey(x, Calc(bAtk, bEnd, bMove, x, bHP, bKBH, bRange, bAOE));
            MaxHealthCurve.AddKey(x,       Calc(bAtk, bEnd, bMove, bKBD, x, bKBH, bRange, bAOE));
            KnockBackHealthCurve.AddKey(x, Calc(bAtk, bEnd, bMove, bKBD, bHP, x, bRange, bAOE));
            HorizontalRangeCurve.AddKey(x, Calc(bAtk, bEnd, bMove, bKBD, bHP, bKBH, x, bAOE));
            AOEEfficiencyCurve.AddKey(x,   Calc(bAtk, bEnd, bMove, bKBD, bHP, bKBH, bRange, x));
        }

        // Max values — Y at x = MaxStatValue
        AttackDamage_MaxValue    = Calc(MaxStatValue, bEnd, bMove, bKBD, bHP, bKBH, bRange, bAOE);
        AttackEndlag_MaxValue    = Calc(bAtk, MaxStatValue, bMove, bKBD, bHP, bKBH, bRange, bAOE);
        MoveSpeed_MaxValue       = Calc(bAtk, bEnd, MaxStatValue, bKBD, bHP, bKBH, bRange, bAOE);
        KnockBackDamage_MaxValue = Calc(bAtk, bEnd, bMove, MaxStatValue, bHP, bKBH, bRange, bAOE);
        MaxHealth_MaxValue       = Calc(bAtk, bEnd, bMove, bKBD, MaxStatValue, bKBH, bRange, bAOE);
        KnockBackHealth_MaxValue = Calc(bAtk, bEnd, bMove, bKBD, bHP, MaxStatValue, bRange, bAOE);
        HorizontalRange_MaxValue = Calc(bAtk, bEnd, bMove, bKBD, bHP, bKBH, MaxStatValue, bAOE);
        AOEEfficiency_MaxValue   = Calc(bAtk, bEnd, bMove, bKBD, bHP, bKBH, bRange, MaxStatValue);
    }
}