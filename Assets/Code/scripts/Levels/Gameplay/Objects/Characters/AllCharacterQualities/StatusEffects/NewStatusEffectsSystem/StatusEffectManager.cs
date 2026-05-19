using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StatusEffectManager : MonoBehaviour
{
    public Stats stats { get; private set; }
    List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();

    [Header("Events")]
    [SerializeField] UnityEvent<StatusEffect> onEffectApplied;
    [SerializeField] UnityEvent<StatusEffect> onEffectRemoved;
    [SerializeField] UnityEvent<StatusEffect> onEffectTick;

    public void Initialize(Stats statsComponent)
    {
        stats = statsComponent;
    }

    void Update()
    {
        ProcessEffects(Time.deltaTime);
    }

    public void AddEffect(StatusEffect effect)
    {
        if (effect == null) return;

        if (effect.CanStack())
        {
            ActiveStatusEffect existing = activeEffects.Find(e => e.effectData == effect);
            if (existing != null)
            {
                existing.stackCount++;
                existing.timeRemaining = effect.duration;
                return;
            }
        }
        else
        {
            ActiveStatusEffect existing = activeEffects.Find(e => e.effectData == effect);
            if (existing != null)
            {
                existing.timeRemaining = effect.duration;
                return;
            }
        }

        ActiveStatusEffect newEffect = effect.CreateInstance();
        activeEffects.Add(newEffect);
        
        effect.OnApply(stats);
        onEffectApplied?.Invoke(effect);
    }

    public void RemoveEffect(StatusEffect effect)
    {
        ActiveStatusEffect active = activeEffects.Find(e => e.effectData == effect);
        if (active != null)
        {
            effect.OnRemove(stats);
            activeEffects.Remove(active);
            onEffectRemoved?.Invoke(effect);
        }
    }

    public void ClearAllEffects()
    {
        foreach (var effect in activeEffects)
        {
            effect.effectData.OnRemove(stats);
            onEffectRemoved?.Invoke(effect.effectData);
        }
        activeEffects.Clear();
    }

    public bool HasEffect(StatusEffect effect)
    {
        return activeEffects.Exists(e => e.effectData == effect);
    }

    public void ApplyEffect(StatusEffect effect, float durationOverride)
    {
        if (effect == null) return;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].effectData == effect)
            {
                var refreshed = activeEffects[i];
                refreshed.timeRemaining = durationOverride;
                activeEffects[i] = refreshed;

                if (effect is IterativeStatusEffect iterativeData && activeEffects[i] is ActiveIterativeEffect iterInstance)
                {
                    iterInstance.nextTickTime = iterativeData.tickInterval;
                    activeEffects[i] = iterInstance;
                }

                return;
            }
        }

        effect.OnApply(stats);
        ActiveStatusEffect instance = effect.CreateInstance();
        instance.timeRemaining = durationOverride;
        activeEffects.Add(instance);
        onEffectApplied?.Invoke(effect);
    }

    public List<ActiveStatusEffect> GetActiveEffects()
    {
        return new List<ActiveStatusEffect>(activeEffects);
    }

    private void ProcessEffects(float deltaTime)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect effect = activeEffects[i];
            
            effect.timeRemaining -= deltaTime;

            if (effect.effectData is IterativeStatusEffect iterative)
            {
                effect.nextTickTime -= deltaTime;
                
                if (effect.nextTickTime <= 0f)
                {
                    for (int stack = 0; stack < effect.stackCount; stack++)
                    {
                        effect.effectData.OnTick(stats, deltaTime);
                    }
                    
                    onEffectTick?.Invoke(effect.effectData);
                    
                    effect.nextTickTime = iterative.tickInterval;
                }
            }
            else
            {
                effect.effectData.OnTick(stats, deltaTime);
            }

            if (effect.IsExpired())
            {
                effect.effectData.OnRemove(stats);
                onEffectRemoved?.Invoke(effect.effectData);
                activeEffects.RemoveAt(i);
            }
        }
    }
}