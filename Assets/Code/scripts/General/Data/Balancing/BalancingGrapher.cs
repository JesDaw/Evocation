using UnityEngine;

[ExecuteInEditMode]
public class BalancingGrapher : MonoBehaviour
{
    [Header("Universal Simulation Settings")]
    public float SimulationDistance = 100f;
    public float MaxStatValue = 100f;
    public float Base_Velocity = 5f;

    [Header("Global Weights")]
    public float Weight_MoveSpeed       = 1.0f;
    public float Weight_KnockBackDamage = 1.0f;
    public float Weight_AttackDamage    = 1.0f;
    public float Weight_AttackEndlag    = 1.0f;
    public float Weight_MaxHealth       = 1.0f;
    public float Weight_KnockBackHealth = 1.0f;
    public float Weight_HorizontalRange = 1.0f;
}