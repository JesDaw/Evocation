using UnityEngine;

public class DamageSource
{
    public bool IsEnemy;
    public bool IsPlayer;
    

    /// <summary>
    /// World-space position of the attacker at the moment damage was dealt.
    /// Used by PlayerKnockedBackState to determine the correct knockback direction.
    /// Remains Vector3.zero when the source has no meaningful position (e.g. status effects).
    /// populated by CombatLogic on every damaging action.
    /// </summary>
    public Vector3 sourcePosition;
    public DamageType damageType;
    public enum DamageType
    {
        StatusEffect,
        Melee,
        Ranged,
        AOE,
        Spell
    }

    // constructors 
    public DamageSource() { }
    public DamageSource(DamageType type) { damageType = type; }
    public DamageSource(DamageType type, Vector3 position) { damageType = type; sourcePosition = position; }
    public DamageSource(bool isEnemy, DamageType type, Vector3 position) { IsEnemy = isEnemy; damageType = type; sourcePosition = position; }
}
