using System.Collections.Generic;
using UnityEngine;
using Evocation.Clans;

/// <summary>
/// ScriptableObject that defines a CPU's stats, appearance, and behavior
/// Assign to CpuStateManager._ScrStats
/// </summary>
[CreateAssetMenu(fileName = "CpuStats", menuName = "CPU/Stats", order = 0)]
public class ScriptableStats : ScriptableObject
{
    [Header("Animation & Appearance")]
    [Tooltip("Should the sprite be rotated 180 degrees?")]
    public bool _Rotate;
    
    [Tooltip("The animator controller for this CPU type")]
    public AnimatorOverrideController _animator;
    
    [Tooltip("Animation rigs for different states (Idle, Running, Attack, Knockback)")]
    public animationRigs[] _Sprites;
    
    [Tooltip("Speed multiplier for movement animation")]
    public float _AnimationMoveSpeed = 1f;

    [Space]
    [Header("General Stats")]
    [Tooltip("Maximum health points")]
    public int _MaxHealth = 1;
    
    [Tooltip("Movement speed")]
    public float _MoveSpeed;
    
    [Tooltip("Cost to spawn this unit")]
    public int _spawnCost;

    [Space]
    [Header("Combat")]
    [Tooltip("The type of attack this CPU uses (Melee, Range, AOE, etc.)")]
    public AttackType _AttackType;
    
    [Space]
    [Header("Knockback")]
    [Tooltip("How many hits before being knocked back")]
    public float _KnockBackMax = 1;
    
    [Tooltip("Force of knockback when triggered")]
    public float _KnockBackVelocity;
}

/// <summary>
/// Container for animation rig data
/// </summary>
[System.Serializable]
public class animationRigs
{
    public enum animationKey 
    { 
        Idle, 
        Running, 
        Attack, 
        Knockback
    }
    
    [Tooltip("Which animation state this rig is for")]
    public animationKey Key;
    
    [Tooltip("The prefab containing the sprite rig")]
    public GameObject Rig;
    
    [Tooltip("Position offset for this rig")]
    public Vector2 Offset;
}