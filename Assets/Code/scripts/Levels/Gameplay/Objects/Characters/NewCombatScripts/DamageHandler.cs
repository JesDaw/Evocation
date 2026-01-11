using UnityEngine;
using UnityEngine.Events;

public class DamageHandler : MonoBehaviour
{
    private Stats stats;

    [Header("Building stuff")]
    [SerializeField] FloatVariable buildingHealthVariable; 
    [SerializeField] bool isMainBase = false;
    [SerializeField] bool isMoneyBuilding = false;
    [SerializeField] UnityEvent onBuildingDestroyed;
    [SerializeField] UnityEvent onMoneyBuildingDestroyed;

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

        if (attackedBy != null && attackedBy.damageType == DamageSource.DamageType.StatusEffect)
        {
            stats._KnockBackHealth--;
        }

        if (attackedBy != null)
        {
            stats.OnWitFlagDamage?.Invoke(attackedBy.IsEnemy);
        }
        stats.OnDamage?.Invoke();

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
        
        if (buildingHealthVariable != null)
        {
            buildingHealthVariable._Value = 0;
        }

        if (stats.LastHitBy != null)
        {
            stats.OnWitFlagDeath?.Invoke(stats.LastHitBy.IsEnemy);
        }

        stats.OnDeath?.Invoke();

        if (isMainBase)
        {
            onBuildingDestroyed?.Invoke();
            Debug.Log("Main base destroyed!");
        }
        else if (isMoneyBuilding)
        {
            onMoneyBuildingDestroyed?.Invoke();
            Debug.Log("Money building destroyed!");
        }

        if (!stats._DontDestroy)
        {
            Destroy(gameObject);
        }
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
        
        if (buildingHealthVariable != null)
        {
            buildingHealthVariable._Value = stats._CurrentHealth;
        }
        stats.OnDamage?.Invoke();
    }

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