using UnityEngine;

/// <summary>
/// Status effects that apply their effect repeatedly over time (DoT, HoT, etc.)
/// </summary>
[CreateAssetMenu(fileName = "New Iterative Effect", menuName = "Status Effects/Iterative Effect")]
public class IterativeStatusEffect : StatusEffect
{
    [Header("Iterative Settings")]
    public float tickInterval = 1f; 
    public float damagePerTick = 5f; 
    public bool canKill = true; // if we want to do minecraft poison maybe

    [Header("Stacking")]
    [SerializeField] private bool allowStacking = false;
    //[SerializeField] private int maxStacks = 3;

    public override void OnApply(Stats target)
    {
        // Could spawn particles, play sound, etc.
        Debug.Log($"{effectName} applied to {target.gameObject.name}");
    }

    public override void OnTick(Stats target, float deltaTime) // This is called by StatusEffectManager
    {
        //For damage and healing
        if (damagePerTick > 0)
        {
            // Damage
            DamageSource source = new DamageSource(DamageSource.DamageType.StatusEffect);
            source.IsEnemy = target._Enemy; // This should be set by whoever applied it
            target.damageHandler.TakeDamage(damagePerTick, source);
        }
        else if (damagePerTick < 0)
        {
            // Healing
            target.damageHandler.Heal(damagePerTick);
            
            // Prevent healing from killing
            // Do we need this? i'll just remove it for now...
            /*
            if (!canKill && target._CurrentHealth < 1f)
            {
                target._CurrentHealth = 1f;
            }
            */
        }
    }

    public override void OnRemove(Stats target)
    {
        Debug.Log($"{effectName} removed from {target.gameObject.name}");
    }

    public override bool CanStack()
    {
        return allowStacking;
    }

    public override ActiveStatusEffect CreateInstance()
    {
        return new ActiveIterativeEffect(this);
    }
}

[System.Serializable]
public class ActiveIterativeEffect : ActiveStatusEffect
{
    public IterativeStatusEffect IterativeData => effectData as IterativeStatusEffect;

    public ActiveIterativeEffect(IterativeStatusEffect effect) : base(effect)
    {
        nextTickTime = effect.tickInterval;
    }
}
