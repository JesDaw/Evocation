using UnityEngine;

[ExecuteInEditMode]
public class BalancingGrapher : MonoBehaviour
{
    [Header("Universal Simulation Settings")]
    public float SimulationDistance = 100f;
    public float Base_Velocity = 5f;

    [Header("Max Stat Values (For Curves)")]
    public float Max_MoveSpeed = 15f;
    public float Max_Endlag = 30f;
    public float Max_Range = 30f;
    public float Max_Health = 100f;
    public float Max_Damage = 100f;
    public float Max_KBDamage = 100f;
    public float Max_KBHealth = 100f;

    [Header("Global Weights")]
    public float Weight_MoveSpeed       = 1.0f;
    public float Weight_KnockBackDamage = 1.0f;
    public float Weight_AttackDamage    = 1.0f;
    public float Weight_AttackEndlag    = 1.0f;
    public float Weight_MaxHealth       = 1.0f;
    public float Weight_KnockBackHealth = 1.0f;
    public float Weight_HorizontalRange = 1.0f;
}