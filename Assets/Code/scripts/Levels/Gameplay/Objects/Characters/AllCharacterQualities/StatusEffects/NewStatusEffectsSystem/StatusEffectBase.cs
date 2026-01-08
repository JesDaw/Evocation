using UnityEngine;

public abstract class StatusEffect : ScriptableObject
{
    [Header("Basic Info")]
    public string effectName = "Status Effect";
    public Sprite icon;
    
    [Header("Duration")]
    public float duration = 5f;
    
    [Header("Visual")]
    public Color effectColor = Color.white;
    public GameObject particleEffectPrefab;

    public virtual void OnApply(Stats target) { }

    public virtual void OnTick(Stats target, float deltaTime) { }
    public virtual void OnRemove(Stats target) { }
    public virtual bool CanStack() { return false; }
    public virtual ActiveStatusEffect CreateInstance()
    {
        return new ActiveStatusEffect(this);
    }
}

[System.Serializable]
public class ActiveStatusEffect
{
    public StatusEffect effectData;
    public float timeRemaining;
    public float nextTickTime;
    public int stackCount = 1;

    public ActiveStatusEffect(StatusEffect effect)
    {
        effectData = effect;
        timeRemaining = effect.duration;
        nextTickTime = 0f;
    }

    public bool IsExpired()
    {
        return timeRemaining <= 0f;
    }
}