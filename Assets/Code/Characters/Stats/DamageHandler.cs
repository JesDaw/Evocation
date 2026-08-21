using UnityEngine;
using UnityEngine.Events;

public class DamageHandler : MonoBehaviour
{
    Stats stats;
    bool DamageTriggerInvoked = false;
    [SerializeField] bool DebugLogs;

    public void Initialize(Stats statsComponent)
    {
        stats = statsComponent;
    }

    public void TakeDamage(float damage, float knockback_damage, DamageSource attackedBy = null)
    {
        if (DebugLogs) Debug.Log($"{gameObject.name}: Taking {damage} damage");
        if (stats == null) return;
        if (stats.IsInvincible()) return;

        stats._CurrentHealth -= damage;
//        Debug.Log($"{gameObject.name} Health = {stats._CurrentHealth}");
        FModAudioManager.instance.PlaySoundByName("takeDamage", transform.position, 1, 15, "Volume", 1f);

        stats.OnDamage?.Invoke();
        if (stats.DamageTriggerAmount >= stats._CurrentHealth && !DamageTriggerInvoked)
        {
            stats.DamageTrigger?.Invoke();
            DamageTriggerInvoked = true;
        }

        stats.LastHitBy = attackedBy;

        if (attackedBy != null)
        {
            stats.OnWitFlagDamage?.Invoke(attackedBy.IsEnemy);
            stats._KnockBackHealth -= knockback_damage;

            Transform target = GetComponentInChildren<AnimationDrivenVFXController>()?.transform;
            if(target is not null)
                ImpactParticleSpawner.Instance.PlaySmallImpactParticle(target.position, Vector3.one, Quaternion.identity);
            //GameObject parent_obj = transform.parent.gameObject;
        }

        if (stats.entityHealthbar != null)
            stats.entityHealthbar.UpdateHealth();

        if (stats._CurrentHealth <= 0)
        {
            Die();
            return;
        }

        if (stats._KnockBackHealth <= 0)
            TriggerKnockback();
    }

    public void TakeDamage(float damage, DamageSource attackedBy = null)
    {
        if (DebugLogs) Debug.Log($"{gameObject.name}: Taking {damage} damage");
        if (stats == null) return;
        if (stats.IsInvincible()) return;

        stats._CurrentHealth -= damage;
        FModAudioManager.instance.PlaySoundByName("takeDamage", transform.position, 1, 15, "Volume", 1f);


        stats.OnDamage?.Invoke();

        stats.LastHitBy = attackedBy;

        if (attackedBy != null && attackedBy.damageType == DamageSource.DamageType.StatusEffect)
            stats._KnockBackHealth--;

        if (attackedBy != null)
        {
            stats.OnWitFlagDamage?.Invoke(attackedBy.IsEnemy);
            if (attackedBy.damageType == DamageSource.DamageType.Spell)
            {
                Transform target = GetComponentInChildren<AnimationDrivenVFXController>()?.transform;
                if(target is not null)
                    ImpactParticleSpawner.Instance.PlayLargeImpactParticle(target.position, Vector3.one, Quaternion.identity);
            }
        }

        if (stats.entityHealthbar != null)
            stats.entityHealthbar.UpdateHealth();

        if (stats._CurrentHealth <= 0)
        {
            Die();
            return;
        }

        if (stats._KnockBackHealth <= 0) TriggerKnockback();
    }

    public void Die()
    {
        if (DebugLogs) Debug.Log($"{gameObject.name}: Dying");
        if (stats == null) return;

        stats._CurrentHealth = 0;
        stats._IsDead = true;

        if (stats.LastHitBy != null)
            stats.OnWitFlagDeath?.Invoke(stats.LastHitBy.IsEnemy);

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

        stats._CurrentHealth = Mathf.Min(
            stats._CurrentHealth + amount,
            stats._MaxHealth
        );

        stats.OnDamage?.Invoke();

        if (stats.entityHealthbar != null)
            stats.entityHealthbar.UpdateHealth();
    }

    public void ResetHealth()
    {
        if (stats == null) return;
        stats._CurrentHealth = stats._MaxHealth;
        if (stats.entityHealthbar != null)
            stats.entityHealthbar.UpdateHealth();
    }

    public bool IsDead()
    {
        return stats != null && stats._CurrentHealth <= 0;
    }
}

