using UnityEngine;

[ExecuteInEditMode]
public class BalancingGrapher : MonoBehaviour
{
    [Header("Universal Simulation Settings")]
    public float SimulationDistance = 100f;
    public float Base_Velocity = 5f;

    [Header("Power Calibration")]
    [Tooltip("The single fixed unit every character's power/cost is compared against. " +
             "Replaces the old 'average of the whole roster' baseline — that approach meant " +
             "editing any one unit's stats could shift every other unit's calculated cost.")]
    public ScriptableStats AnchorUnit;

    [Header("Max Stat Values (For Curves)")]
    public float Max_MoveSpeed = 15f;
    public float Max_Endlag = 30f;
    public float Max_Range = 30f;
    public float Max_Health = 100f;
    public float Max_Damage = 100f;
    public float Max_KBDamage = 100f;
    public float Max_KBHealth = 100f;

    [Header("Global Weights")]
    // Weight_AttackEndlag now scales the full effective cooldown
    // (PowerMath.GetEffectiveCooldown = animation time * ActionCooldown + ExtraEndlag),
    // not a single raw field — see chat notes on the cooldown fix.
    public float Weight_MoveSpeed       = 1.0f;
    public float Weight_KnockBackDamage = 1.0f;
    public float Weight_AttackDamage    = 1.0f;
    public float Weight_AttackEndlag    = 1.0f;
    public float Weight_MaxHealth       = 1.0f;
    public float Weight_KnockBackHealth = 1.0f;
    public float Weight_HorizontalRange = 1.0f;
    [Tooltip("Diminishing-returns weight for hitting multiple targets (CombatAction.maxTargets). " +
             "0 = AOE gets no bonus. Kept modest on purpose — see chat notes on why AOE should be " +
             "priced for typical mixed combat, not its swarm-clearing best case.")]
    public float Weight_AOE = 0.2f;

    [Header("Stat Parity Constants (used by LevelBalancerMath only)")]
    public float K_MoveSpeed = 1f;
    public float K_Range = 1f;
    public float K_AttackRate = 1f;

    // Removed: W_Attack, W_Health, W_KB_Damage, W_KB_Health.
    // These were declared here but never referenced by any script in the
    // project as provided — dead fields. If you were setting these in the
    // Inspector expecting them to affect balance, they weren't doing
    // anything; let me know and I'll wire them in properly instead.
}