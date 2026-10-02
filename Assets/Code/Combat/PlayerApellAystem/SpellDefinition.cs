using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpellCastMode
{
    Aimed,
    Instant,
    SelfCast
}

[CreateAssetMenu(menuName = "Spells/Spell Definition", fileName = "New Spell")]
public class SpellDefinition : ScriptableObject
{
    [Header("Info")]
    public string SpellName;
    public Sprite Icon;

    [TextArea]
    public string Description;

    [Header("Cast")]
    public SpellCastMode castMode = SpellCastMode.Aimed;
    public uint Cost;

    [Tooltip("Size of the target selection circle. Unused for SelfCast.")]
    public float Radius = 2f;

    [Header("Effect")]
    [Tooltip("Dumbed-down, spell-only version of CombatAction. Translated into a real CombatAction at runtime.")]
    public SpellEffectData spellEffect;

    CombatAction _runtimeAction;
    CombatAction Action => _runtimeAction ??= spellEffect.ToCombatAction();

    [Header("Timing")]
    public float hitboxDelay = 0.5f;
    public float animationDuration = 1f;

    [Header("Presentation")]
    public GameObject spellVFX;
    public string castSoundName = "explosion";

    [SerializeField] protected bool DebugLogs = false;

    public virtual IEnumerator RunCastSequence(SpellCaster caster, Vector3 castPosition)
    {
        FModAudioManager.instance.PlaySoundByName(castSoundName);

        GameObject vfx = spellVFX != null ? Instantiate(spellVFX, castPosition, Quaternion.identity) : null;

        float elapsed = 0f;

        while (elapsed < hitboxDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ApplySpellEffect(caster, castPosition);

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (vfx != null) Destroy(vfx);
    }

    protected void ApplySpellEffect(SpellCaster caster, Vector3 castPosition)
    {
        Stats casterStats = caster.CasterStats;

        if (casterStats == null)
        {
            Debug.LogError($"[{SpellName}] CasterStats is NULL.");
            return;
        }

        if (spellEffect == null)
        {
            Debug.LogError($"[{SpellName}] SpellEffectData is NULL on the SpellDefinition.");
            return;
        }

        if (castMode == SpellCastMode.SelfCast)
        {
            CombatLogic.ExecuteActionOnTarget(casterStats, Action, casterStats);
        }
        else
        {
            CombatLogic.ExecuteActionAtPosition(casterStats, Action, castPosition, Radius);
        }

        if (DebugLogs) Debug.Log($"{SpellName} resolved at {castPosition}");

        OtherEffects();
    }

    protected virtual void OtherEffects() {}
}

[System.Serializable]
public class SpellEffectData
{
    [Header("Targeting")]
    public bool targetFriendly = false;
    public int maxTargets = 1;

    [Header("Effect")]
    [Tooltip("Negative = damage, positive = healing.")]
    public float healthChange = -20f;
    public float knockback = 0f;
    public List<StatusEffect> effectsOnHit = new();

    [Header("Zone (optional)")]
    public AreaEffectData zoneData;
    public SpellZoneMode zoneMode = SpellZoneMode.None;
    public bool zoneSticky = false;
    public bool excludeCasterFromZone = true;

    public CombatAction ToCombatAction() => new CombatAction
    {
        actionName        = "Spell",
        targetFriendly    = targetFriendly,
        maxTargets        = maxTargets,
        ScaleEffectsWithUsersStats = false,
        healthChangePercent  = healthChange,
        knockbackPercent = knockback,
        effectsOnHit      = effectsOnHit,
        zoneData          = zoneData,
        zoneSpawnPosition = zoneMode == SpellZoneMode.AroundCastPoint ? ZoneSpawnPosition.Self
                           : zoneMode == SpellZoneMode.OnEachTarget   ? ZoneSpawnPosition.Touch
                           : ZoneSpawnPosition.Self,
        zoneSticky        = zoneSticky,
        excludeCasterFromZone = excludeCasterFromZone,
    };
}

public enum SpellZoneMode { None, AroundCastPoint, OnEachTarget }