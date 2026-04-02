using UnityEngine;

[ExecuteInEditMode]
public class BalancingGrapher : MonoBehaviour
{
    [Header("Master Curve")]
    public AnimationCurve TotalPowerSum = new();

    [Header("Graphs")]
    public AnimationCurve MoveSpeedCurve = new();
    public float Weight_MoveSpeed = 0.4f;
    public AnimationCurve KBDamageCurve = new();
    public float Weight_KnockBackDamage = 0.3f;
    public AnimationCurve AttackDamageCurve = new();
    public float Weight_AttackDamage = 1.0f;

    public AnimationCurve HealthCurve = new();
    public float Weight_MaxHealth = 0.5f;

    public AnimationCurve KBHealthCurve = new();
    public float Weight_KnockBackHealth = 0.3f;

    public AnimationCurve RangeCurve = new();
    public float Weight_HorizontalRange = 0.2f;

    public AnimationCurve EndlagPenaltyCurve = new();
    public float Weight_AttackEndlag = 1.0f; 
    public AnimationCurve AOECurve = new();
    public float Weight_AOE_Efficiency = 0.2f;

    [Header("Graph Settings")]
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
            
            float pEnd = (pAtk * Weight_AttackDamage) / (Mathf.Max(x * Weight_AttackEndlag, 0.01f) + 0.5f);
            
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