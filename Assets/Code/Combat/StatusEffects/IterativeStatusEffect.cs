using UnityEngine.Events;
using UnityEngine;

[CreateAssetMenu(fileName = "New Iterative Effect", menuName = "Status Effects/Iterative Effect")]
public class IterativeStatusEffect : StatusEffect
{
    [Header("Iterative Settings")]
    public float tickInterval = 1f; 
    public float HealthChangePerTick = 5f; 
    public bool canKill = true; 

    [Header("Stacking")]
    [SerializeField] bool allowStacking = false;
    //[SerializeField] private int maxStacks = 3;

    public override void OnApply(Stats target)
    {
        // Could spawn particles, play sound, etc.
        Debug.Log($"{effectName} applied to {target.gameObject.name}");
    }

    public override void OnTick(Stats target, float deltaTime) //called by StatusEffectManager
    {
        target.AlterHealth(HealthChangePerTick);
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
public class ActiveIterativeEffect : ActiveStatusEffect // what is this for?
{
    public UnityEvent<Stats> specialOnTick;
    public IterativeStatusEffect IterativeData => effectData as IterativeStatusEffect;

    public ActiveIterativeEffect(IterativeStatusEffect effect) : base(effect)
    {
        nextTickTime = effect.tickInterval;
    }
}