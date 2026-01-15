using UnityEngine;
using UnityEngine.Events;

public class DamageHandler : MonoBehaviour
{
    Stats stats;

    public void Initialize(Stats statsComponent)
    {
        stats = statsComponent;
    }


    public void TakeDamage(float damage, DamageSource attackedBy = null)
    {
        if (stats == null) return;
        if (stats.IsInvincible()) return;

        stats._CurrentHealth -= damage;

        stats.OnDamage?.Invoke();

        stats.LastHitBy = attackedBy;

        if (attackedBy != null && attackedBy.damageType == DamageSource.DamageType.StatusEffect)
        {
            stats._KnockBackHealth--;
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

        if (stats._KnockBackHealth > 0 && stats._KnockBackHealth <= 0)
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
        stats.OnDeath?.Invoke();
        stats.OnKnocked?.Invoke(); // Trigger knockback animation before destruction
    }

    private void TriggerKnockback()
    {
        stats._KnockBackHealth = stats._KnockBackMax;
        stats.OnKnocked?.Invoke();
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