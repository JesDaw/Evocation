using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages all active status effects on a character
/// </summary>
public class StatusEffectManager : MonoBehaviour
{
    private Stats stats;
    private List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();

    [Header("Events")]
    [SerializeField] private UnityEvent<StatusEffect> onEffectApplied;
    [SerializeField] private UnityEvent<StatusEffect> onEffectRemoved;
    [SerializeField] private UnityEvent<StatusEffect> onEffectTick;

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