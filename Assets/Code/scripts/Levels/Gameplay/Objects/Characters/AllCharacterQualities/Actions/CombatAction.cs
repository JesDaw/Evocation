using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CombatAction
{
    [Header("Identity")]
    public string actionName = "Action";

    [Header("Priority")]
    [Tooltip("Higher = preferred when multiple actions are candidates. Must be unique per character.")]
    public int priority = 0;

    [Header("Detection")]
    [Tooltip("Multiplier on character horizontal range stat. 1.0 = same as base range, 2.0 = double")]
    public float rangePercent = 1f;
    [Tooltip("If true, the detection circle is offset forward in the facing direction")]
    public bool extendsForward = false;

    [Header("Targeting")]
    [Tooltip("If true, targets allies of the caster. If false, targets enemies")]
    public bool targetFriendly = false;
    public ActionTargetCondition targetCondition = ActionTargetCondition.All;
    [Tooltip("If > 1, hits multiple targets in range (AOE). 1 = single target only")]
    public int maxTargets = 1;
    [Tooltip("Seconds before this action can fire again")]
    public float castCooldown = 2f;

    [Header("Health Change")]
    [Tooltip("Multiplier on character attack damage. Negative = damage, Positive = healing, 0 = no change")]
    public float healthChangePercent = 0f;

    [Header("Knockback")]
    [Tooltip("Multiplier on character knockback damage stat. Positive = knockback, Negative = knockback healing, 0 = no knockback")]
    public float knockbackPercent = 0f;

    [Header("Delivery")]
    [Tooltip("Self = zone spawns at caster (aura). Touch = melee zone on target. Projectile = fires projectile toward target")]
    public ZoneSpawnPosition zoneSpawnPosition = ZoneSpawnPosition.Self;
    [Tooltip("Projectile settings — used only when zoneSpawnPosition is Projectile")]
    public ProjectileSettings projectileSettings;

    [Header("Effects")]
    [Tooltip("Status effects applied to each target")]
    public List<StatusEffect> effectsOnHit = new List<StatusEffect>();

    [Header("Zone")]
    public AreaEffectData zoneData;
    [Tooltip("If true, the zone sticks to the target it spawns on (overrides zoneData.sticky)")]
    public bool zoneSticky = false;
    [Tooltip("If true, the caster is excluded from the zone's effect list")]
    public bool excludeCasterFromZone = true;
    public ZoneApplicationMode applicationMode = ZoneApplicationMode.All;
}

[System.Serializable]
public class ProjectileSettings
{
    public GameObject prefab;
    [Range(0, 100)] public float speed = 15f;
    [Range(0, 10)] public float maxHeight = 2f;
    public AnimationCurve trajectoryCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    public AnimationCurve axisCorrectionCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve speedCurve = AnimationCurve.Constant(0, 1, 1);
}

public enum ActionTargetCondition { All, NotAlreadyAffected }
public enum ZoneSpawnPosition { Self, Touch, Projectile }
public enum ZoneApplicationMode { All, Random }
public enum ZoneShape { Box, Circle }