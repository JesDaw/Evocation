using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, DamageSource source);
    GameObject gameObject { get; }
    Transform transform { get; }
}