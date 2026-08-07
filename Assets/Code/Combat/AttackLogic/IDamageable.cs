using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, float knockback_damage, DamageSource source);
    void TakeDamage(float damage, DamageSource source);

    GameObject gameObject { get; }
    Transform transform { get; }
}