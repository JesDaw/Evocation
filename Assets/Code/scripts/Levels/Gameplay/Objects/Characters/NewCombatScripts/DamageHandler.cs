using UnityEngine;
using UnityEngine.Events;

public class DamageHandler : MonoBehaviour
{
    private Stats stats;

    [Header("Building-Specific (Optional)")]
    [SerializeField] private FloatVariable buildingHealthVariable; 
    [SerializeField] private bool isMainBase = false;
    [SerializeField] private bool isMoneyBuilding = false;
    [SerializeField] private UnityEvent onBuildingDestroyed;
    [SerializeField] private UnityEvent onMoneyBuildingDestroyed;

    public void Initialize(Stats statsComponent)
    {
        stats = statsComponent;
        
        if (buildingHealthVariable != null)
        {
            buildingHealthVariable._Value = stats._MaxHealth;
        }
    }


    public void TakeDamage(float damage, DamageSource attackedBy = null)
    {
        if (stats == null) return;
        if (stats.IsInvincible()) return;

        stats._CurrentHealth -= damage;
        
        if (buildingHealthVariable != null)
        {
            buildingHealthVariable._Value = stats._CurrentHealth;
        }

        stats.LastHitBy = attackedBy;

        // Handle knockback health for status effects (not used by buildings)
        if (attackedBy != null && attackedBy.damageType == DamageSource.DamageType.StatusEffect)
        {
            stats._KnockBackHealth--;
        }

        // Trigger damage events
        if (attackedBy != null)
        {
            stats.OnWitFlagDamage?.Invoke(attackedBy.IsEnemy);
        }
        stats.OnDamage?.Invoke();

        // Check for death
        if (stats._CurrentHealth <= 0)
        {
            Die();
            return;
        }

        // Check for knockback (only for units, not buildings)
        if (stats._KnockBackHealth > 0 && stats._KnockBackHealth <= 0)
        {
            TriggerKnockback();
        }
    }

    /// <summary>
    /// Death/destruction handler - works for all entity types
    /// </summary>
    public void Die()
    {
        if (stats == null) return;

        stats._CurrentHealth = 0;
        
        // Sync with FloatVariable if building uses it
        if (buildingHealthVariable != null)
        {
            buildingHealthVariable._Value = 0;
        }

        // Trigger death events with flag info
        if (stats.LastHitBy != null)
        {
            stats.OnWitFlagDeath?.Invoke(stats.LastHitBy.IsEnemy);
        }

        stats.OnDeath?.Invoke();

        // Handle building-specific death
        if (isMainBase)
        {
            onBuildingDestroyed?.Invoke(); // End game
            Debug.Log("Main base destroyed!");
        }
        else if (isMoneyBuilding)
        {
            onMoneyBuildingDestroyed?.Invoke(); // Change money generation
            Debug.Log("Money building destroyed!");
        }

        // Destroy unless flagged not to
        if (!stats._DontDestroy)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Trigger knockback (units only, buildings don't get knocked back)
    /// </summary>
    private void TriggerKnockback()
    {
        stats._KnockBackHealth = stats._KnockBackMax;
        stats.OnKnocked?.Invoke();
    }

    /// <summary>
    /// Heal entity - works for all types
    /// </summary>
    public void Heal(float amount) 
    {
        if (stats == null) return;
        
        stats._CurrentHealth = Mathf.Min(stats._CurrentHealth + amount, stats._MaxHealth);
        
        // Sync with FloatVariable if building uses it
        if (buildingHealthVariable != null)
        {
            buildingHealthVariable._Value = stats._CurrentHealth;
        }

        // Trigger damage event to update health bars
        stats.OnDamage?.Invoke();
    }

    /// <summary>
    /// Reset health to max (for respawning/reusing entities)
    /// </summary>
    public void ResetHealth()
    {
        if (stats == null) return;
        
        stats._CurrentHealth = stats._MaxHealth;
        
        if (buildingHealthVariable != null)
        {
            buildingHealthVariable.Reset();
            stats._CurrentHealth = buildingHealthVariable._Value;
        }
    }

    /// <summary>
    /// Check if entity is dead
    /// </summary>
    public bool IsDead()
    {
        return stats != null && stats._CurrentHealth <= 0;
    }
}

/// <summary>
/// Information about the source of damage
/// Helps determine behavior (knockback, effects, etc.)
/// </summary>
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