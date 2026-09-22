using UnityEngine;
public interface IDamageable // this seems stupid
{
    void TakeDamage(float damage, float knockback_damage, DamageSource source);
    GameObject gameObject { get; }
    Transform transform { get; }
}