using UnityEngine;

[CreateAssetMenu(fileName = "New Static Effect", menuName = "Status Effects/Static Effect")]
public class StaticStatusEffect : StatusEffect
{
    [Header("Stat Modifiers")]
    public float moveSpeedMultiplier = 1f;
    public float moveSpeedFlat = 0f;

    public float attackDamageMultiplier = 1f;
    public float attackDamageFlat = 0f;

    public float knockbackDamageMultiplier = 1f;
    public float knockbackDamageFlat = 0f;

    public float horizontalRangeMultiplier = 1f;

    public float animationSpeedMultiplier = 1f;

    [Header("Stacking")]
    [SerializeField] private bool allowStacking = false;

    public override void OnApply(Stats target)
    {
        if (!target._EffectSnapshots.TryGetValue(this, out var snap))
        {
            snap = new StaticEffectSnapshot
            {
                moveSpeed = target._MoveSpeed,
                attackDamage = target._AttackDamage,
                knockbackDamage = target._KnockBackDamage,
                horizontalRange = target._AttackRange.x,
                animationSpeed = target.animator != null ? target.animator.speed : 1f,
                stackCount = 0
            };
            target._EffectSnapshots[this] = snap;
        }

        snap.stackCount++;
        ApplyAllModifiers(target, snap);

        Debug.Log($"{effectName} applied to {target.gameObject.name} (stacks: {snap.stackCount})");
    }

    public override void OnTick(Stats target, float deltaTime)
    {
    }

    public override void OnRemove(Stats target)
    {
        if (!target._EffectSnapshots.TryGetValue(this, out var snap)) return;

        snap.stackCount--;
        if (snap.stackCount > 0)
        {
            ApplyAllModifiers(target, snap);
        }
        else
        {
            target._MoveSpeed = snap.moveSpeed;
            target._AttackDamage = snap.attackDamage;
            target._KnockBackDamage = snap.knockbackDamage;
            target._AttackRange = new Vector2(snap.horizontalRange, target._AttackRange.y);
            if (target.animator != null)
                target.animator.speed = snap.animationSpeed;
            target._EffectSnapshots.Remove(this);
        }

        Debug.Log($"{effectName} removed from {target.gameObject.name}");
    }

    void ApplyAllModifiers(Stats target, StaticEffectSnapshot snap)
    {
        target._MoveSpeed = (snap.moveSpeed * moveSpeedMultiplier) + moveSpeedFlat;
        target._AttackDamage = Mathf.RoundToInt((snap.attackDamage * attackDamageMultiplier) + attackDamageFlat);
        target._KnockBackDamage = (snap.knockbackDamage * knockbackDamageMultiplier) + knockbackDamageFlat;
        target._AttackRange = new Vector2(
            snap.horizontalRange * horizontalRangeMultiplier,
            target._AttackRange.y);

        if (target.animator != null)
            target.animator.speed = snap.animationSpeed * animationSpeedMultiplier;
    }

    public override bool CanStack()
    {
        return allowStacking;
    }
}