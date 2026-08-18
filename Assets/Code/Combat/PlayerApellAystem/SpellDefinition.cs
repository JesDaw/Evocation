using System.Collections;
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
    [Tooltip("Same data CPU combat actions use - targeting, health change, knockback, status effects, zones.")]
    public CombatAction action;

    [Header("Timing")]
    public float hitboxDelay = 0.5f;
    public float animationDuration = 1f;

    [Header("Presentation")]
    public GameObject spellVFX;
    public string castSoundName = "explosion";

    [SerializeField] protected bool DebugLogs = false;

    public virtual IEnumerator RunCastSequence(
        SpellCaster caster,
        Vector3 castPosition)
    {
        FModAudioManager.instance.PlaySoundByName(castSoundName);

        GameObject vfx = spellVFX != null
            ? Instantiate(spellVFX, castPosition, Quaternion.identity)
            : null;

        float elapsed = 0f;

        while (elapsed < hitboxDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ResolveHit(caster, castPosition);

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (vfx != null)
            Destroy(vfx);
    }

    protected void ResolveHit(
        SpellCaster caster,
        Vector3 castPosition)
    {
        Stats casterStats = caster.CasterStats;

        if (casterStats == null)
        {
            Debug.LogError($"[{SpellName}] CasterStats is NULL.");
            return;
        }

        if (action == null)
        {
            Debug.LogError(
                $"[{SpellName}] CombatAction is NULL on the SpellDefinition."
            );
            return;
        }

        if (castMode == SpellCastMode.SelfCast)
        {
            CombatLogic.ExecuteActionOnTarget(
                casterStats,
                action,
                casterStats
            );
        }
        else
        {
            CombatLogic.ExecuteActionAtPosition(
                casterStats,
                action,
                castPosition,
                Radius
            );
        }

        if (DebugLogs)
            Debug.Log($"{SpellName} resolved at {castPosition}");
    }
}