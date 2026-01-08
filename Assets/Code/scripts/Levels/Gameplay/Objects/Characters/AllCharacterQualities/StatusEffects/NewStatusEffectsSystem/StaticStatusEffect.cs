using UnityEngine;

/// <summary>
/// Status effects that modify stats for their duration
/// </summary>
[CreateAssetMenu(fileName = "New Static Effect", menuName = "Status Effects/Static Effect")]
public class StaticStatusEffect : StatusEffect
{
    [Header("Stat Modifications")]
    public float moveSpeedMultiplier = 1f; 
    public float moveSpeedFlat = 0f; // this is for if we want to add of subtract instead
    
    public float attackSpeedMultiplier = 1f;
    public float attackDamageMultiplier = 1f;
    public float attackDamageFlat = 0f; // same for this

    [Header("Stacking")]
    [SerializeField] private bool allowStacking = false;

    private float originalMoveSpeed;

    public override void OnApply(Stats target)
    {
        originalMoveSpeed = target._MoveSpeed;

        ApplyModifications(target);
        
        Debug.Log($"{effectName} applied to {target.gameObject.name}");
    }

    public override void OnTick(Stats target, float deltaTime)
    {
        //  we can use this for visual effects if we do that
    }

    public override void OnRemove(Stats target)
    {
        RemoveModifications(target);
        
        Debug.Log($"{effectName} removed from {target.gameObject.name}");
    }

    private void ApplyModifications(Stats target)
    {
        target._MoveSpeed = (target._MoveSpeed * moveSpeedMultiplier) + moveSpeedFlat;
    }

    private void RemoveModifications(Stats target)
    {
        // this doesnt work for stacking yet
        target._MoveSpeed = originalMoveSpeed;
    }

    public override bool CanStack()
    {
        return allowStacking;
    }
}