using UnityEngine;

[ExecuteInEditMode]
public class BalancingGrapher : MonoBehaviour
{
    [Header("--- 1. THE MASTER SUM ---")]
    public AnimationCurve TotalPowerSum = new();

    [Header("--- 2. LINEAR STAT GRAPHS ---")]
    public AnimationCurve MoveSpeedCurve = new();
    public AnimationCurve KBDamageCurve = new();
    public AnimationCurve AttackDamageCurve = new();
    public AnimationCurve HealthCurve = new();
    public AnimationCurve KBHealthCurve = new();
    public AnimationCurve RangeCurve = new();

    [Header("--- 3. NON-LINEAR / MULTIPLIER GRAPHS ---")]
    public AnimationCurve EndlagPenaltyCurve = new();
    public AnimationCurve AOECurve = new();

    [Header("--- 4. STAT WEIGHTS (Edit Here) ---")]
    public float Weight_AttackDamage = 1.0f;
    public float Weight_AttackEndlag = 1.0f; 
    public float Weight_AOE_Efficiency = 0.2f;
    [Space]
    public float Weight_MaxHealth = 0.5f;
    public float Weight_KnockBackHealth = 0.3f;
    public float Weight_HorizontalRange = 0.2f;
    [Space]
    public float Weight_MoveSpeed = 0.4f;
    public float Weight_KnockBackDamage = 0.3f;

    [Header("--- 5. GRAPH SETTINGS ---")]
    public int Resolution = 50; 
    public float MaxStatValue = 100f; 

    public void UpdateGraphs()
    {
        // Reset Curves
        TotalPowerSum = new(); MoveSpeedCurve = new(); KBDamageCurve = new();
        AttackDamageCurve = new(); HealthCurve = new(); KBHealthCurve = new();
        RangeCurve = new(); EndlagPenaltyCurve = new(); AOECurve = new();

        for (int i = 0; i <= Resolution; i++)
        {
            float x = (i / (float)Resolution) * MaxStatValue;

            // Offense
            float pMove = x * Weight_MoveSpeed;
            float pKBD = x * Weight_KnockBackDamage;
            float pAtk = x * Weight_AttackDamage;
            
            // Non-Linear: Hyperbolic Endlag (Using 10 as a base damage for visual)
            float pEnd = (10 * Weight_AttackDamage) / (Mathf.Max(x * Weight_AttackEndlag, 0.01f) + 0.5f);
            
            // Defense
            float pHP = x * Weight_MaxHealth;
            float pKBH = x * Weight_KnockBackHealth;
            float pRange = x * Weight_HorizontalRange;
            
            // Multiplier: AOE (Using 10 as a base power for visual)
            float pAOE = 10 * (1 + (Mathf.Max(x, 1) - 1) * Weight_AOE_Efficiency);

            // Add Keys
            MoveSpeedCurve.AddKey(x, pMove);
            KBDamageCurve.AddKey(x, pKBD);
            AttackDamageCurve.AddKey(x, pAtk);
            HealthCurve.AddKey(x, pHP);
            KBHealthCurve.AddKey(x, pKBH);
            RangeCurve.AddKey(x, pRange);
            EndlagPenaltyCurve.AddKey(x, pEnd);
            AOECurve.AddKey(x, pAOE);

            // Total Sum Calculation
            float total = pMove + pKBD + pAtk + pEnd + pHP + pKBH + pRange;
            TotalPowerSum.AddKey(x, total);
        }
    }
}