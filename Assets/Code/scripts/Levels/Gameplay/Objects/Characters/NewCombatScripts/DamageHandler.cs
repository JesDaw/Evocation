using UnityEngine;
using UnityEngine.Events;
using FMODUnity;
using FMOD.Studio;

public class DamageHandler : MonoBehaviour
{
    [SerializeField] StudioEventEmitter gettingHitEventEmitter;

    Stats stats;
    bool DamageTriggerInvoked = false;
    public void Initialize(Stats statsComponent)
    {
        stats = statsComponent;
    }


    public void TakeDamage(float damage, float knockback_damage, DamageSource attackedBy = null)
    {
        Debug.Log("Takedamage called");
        if (stats == null) return;
        if (stats.IsInvincible()) return;

        stats._CurrentHealth -= damage;

         if (gettingHitEventEmitter != null) gettingHitEventEmitter.Play();
         else Debug.LogWarning($"No gettingHitEventEmitter for audio assigned on {gameObject.name}");

        stats.OnDamage?.Invoke();
        if (stats.DamageTriggerAmount >= stats._CurrentHealth && !DamageTriggerInvoked) 
        {
            //Debug.Log("Activating damage trigger event");
            stats.DamageTrigger?.Invoke();
            DamageTriggerInvoked = true;
        }

        stats.LastHitBy = attackedBy;

        // if (attackedBy != null && attackedBy.damageType == DamageSource.DamageType.StatusEffect)
        // {
             // stats._KnockBackHealth -= knockback_damage;
        // }

        if (attackedBy != null)
        {
            stats.OnWitFlagDamage?.Invoke(attackedBy.IsEnemy);
            stats._KnockBackHealth -= knockback_damage; 

            GameObject parent_obj = transform.parent.gameObject;
            //Debug.Log(knockback_damage + " knockback damage taken by: " + parent_obj);
        }

        if (stats.entityHealthbar != null)
        {
            stats.entityHealthbar.UpdateHealth();
        }

        if (stats._CurrentHealth <= 0)
        {
            Die();
            return;
        }

        if (stats._KnockBackHealth <= 0)
        {
            TriggerKnockback();
        }
    }

    public void TakeDamage(float damage, DamageSource attackedBy = null) // why is there 2 referances to this?
    {
        Debug.Log("Takedamage called");
        if (stats == null) return;
        if (stats.IsInvincible()) return;

        stats._CurrentHealth -= damage;
        FModAudioManager.instance.PlaySoundByName("takeDamage");

        stats.OnDamage?.Invoke();

        stats.LastHitBy = attackedBy;

        if (attackedBy != null && attackedBy.damageType == DamageSource.DamageType.StatusEffect)
        {
            stats._KnockBackHealth-- ; 
        }

        if (attackedBy != null)
        {
            stats.OnWitFlagDamage?.Invoke(attackedBy.IsEnemy);
        }

        if (stats.entityHealthbar != null)
        {
            stats.entityHealthbar.UpdateHealth();
        }

        if (stats._CurrentHealth <= 0)
        {
            Die();
            return;
        }

        if (stats._KnockBackHealth <= 0)
        {
            TriggerKnockback();
        }
    }


    public void Die()
    {
        if (stats == null) return;

        stats._CurrentHealth = 0;
        stats._IsDead = true;
        

        if (stats.LastHitBy != null)
        {
            stats.OnWitFlagDeath?.Invoke(stats.LastHitBy.IsEnemy);
        }
        TriggerKnockback();
        stats.OnDeath?.Invoke();

    }

    private void TriggerKnockback()
    {
        stats.OnKnocked?.Invoke();
        stats._KnockBackHealth = stats._KnockBackMaxHealth;

    }

    public void Heal(float amount)
    {
        if (stats == null) return;
        stats._CurrentHealth = Mathf.Min(stats._CurrentHealth + amount, stats._MaxHealth);
        if (stats.entityHealthbar != null)
        {
            stats.entityHealthbar.UpdateHealth();
        }
    }

    public void ResetHealth()
    {
        if (stats == null) return;
        stats._CurrentHealth = stats._MaxHealth;
        if (stats.entityHealthbar != null)
        {
            stats.entityHealthbar.UpdateHealth();
        }
    }

    public bool IsDead()
    {
        return stats != null && stats._CurrentHealth <= 0;
    }
}

public class DamageSource
{
    public bool IsEnemy;
    public DamageType damageType;

    public enum DamageType 
    { 
        StatusEffect, 
        Melee, 
        Ranged, 
        AOE 
    }

    public DamageSource() { }
    public DamageSource(DamageType type) 
    { 
        damageType = type; 
    }
}