using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpellCastMode
{
    Aimed,
    Instant,
    SelfCast
}

[CreateAssetMenu(menuName = "Spells/New Spell", fileName = "New Spell")]
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
    CombatAction Action => spellEffect.ToCombatAction();

    [Header("Timing")]
    public float hitboxDelay = 0.5f;
    public float animationDuration = 1f;

    [Header("Presentation")]
    public GameObject spellVFX;
    public string castSoundName = "explosion";

    [Header("Special effect")]
    [Tooltip("Theme color: tints the screen-edge vignette and the hero's glow when casting.")]
    public Color hypeColor = new Color(0.55f, 0.35f, 1f);
    [Tooltip("How strongly the hero glows in corresponding color when the cast starts. 0 = off.")]
    [Range(0f, 1f)] public float casterGlowStrength = 0.8f;
    [Tooltip("Optional particles attached to the hero for the whole cast (e.g. swirling magic).")]
    public GameObject casterAuraVFX;
    [Tooltip("Peak strength of the colored screen-edge vignette. Builds up until impact, then fades. 0 = off.")]
    [Range(0f, 1f)] public float vignetteStrength = 0.6f;
    [Tooltip("Camera shake on impact. 0 = off.")]
    public float impactShakeForce = 0.6f;
    [Tooltip("Game speed right at impact, easing back to normal. 1 = no slow-mo.")]
    [Range(0.05f, 1f)] public float impactSlowMoScale = 0.35f;
    [Tooltip("Seconds (real time) for the slow-mo to ease back to normal speed.")]
    public float impactSlowMoDuration = 0.3f;
    [Tooltip("Spawn the large impact particle burst where the spell lands.")]
    public bool impactBurst = true;

    [SerializeField] protected bool DebugLogs = false;

    static readonly int VignetteStrengthID = Shader.PropertyToID("_VignetteStrength");
    static readonly int VignetteColorID = Shader.PropertyToID("_VignetteColor");

    public virtual IEnumerator RunCastSequence(SpellCaster caster, Vector3 castPosition)
    {
        FModAudioManager.instance.PlaySoundByName(castSoundName);

        GameObject vfx = spellVFX != null ? Instantiate(spellVFX, castPosition, Quaternion.identity) : null;

        float end = Mathf.Max(animationDuration, hitboxDelay);
        float fadeOut = Mathf.Max(end - hitboxDelay, 0.01f);
        bool hitResolved = false;
        float elapsed = 0f;

        try
        {
            if (vignetteStrength > 0f) Shader.SetGlobalColor(VignetteColorID, hypeColor);

            while (elapsed < end)
            {
                if (!hitResolved && elapsed >= hitboxDelay)
                {
                    ApplySpellEffect(caster, castPosition);
                    PlayImpactHype(castPosition);
                    hitResolved = true;
                }

                // Vignette builds up to impact, then fades out over the rest of the cast.
                if (vignetteStrength > 0f)
                {
                    float v = elapsed < hitboxDelay
                        ? Mathf.SmoothStep(0f, 1f, hitboxDelay > 0f ? elapsed / hitboxDelay : 1f)
                        : 1f - Mathf.SmoothStep(0f, 1f, (elapsed - hitboxDelay) / fadeOut);
                    Shader.SetGlobalFloat(VignetteStrengthID, v * vignetteStrength);
                }

                // Slow-mo hits hardest at impact, then eases back to normal speed.
                if (hitResolved && impactSlowMoScale < 1f && impactSlowMoDuration > 0f)
                {
                    float t = Mathf.Clamp01((elapsed - hitboxDelay) / impactSlowMoDuration);
                    UILogic.RequestSpellTimeScale(Mathf.Lerp(impactSlowMoScale, 1f, t * t));
                }

                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            if (!hitResolved)
            {
                ApplySpellEffect(caster, castPosition);
                PlayImpactHype(castPosition);
            }
        }
        finally
        {
            if (impactSlowMoScale < 1f) UILogic.ClearSpellTimeScale();
            if (vignetteStrength > 0f)
            {
                Shader.SetGlobalFloat(VignetteStrengthID, 0f);
                Shader.SetGlobalColor(VignetteColorID, Color.black);
            }
            if (vfx != null) Destroy(vfx);
        }
    }

    protected void PlayImpactHype(Vector3 castPosition)
    {
        if (impactShakeForce > 0f && CameraEffects.Instance != null) CameraEffects.Instance.Shake(impactShakeForce);
        if (impactBurst && ImpactParticleSpawner.Instance != null)
            ImpactParticleSpawner.Instance.PlayLargeImpactParticle(castPosition, Vector3.one, Quaternion.identity);
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
            CombatLogic.ExecuteActionOnSelf(casterStats, Action);
        }
        else
        {
            Debug.Log(Action.healthChangePercent);
            CombatLogic.ExicuteCombatAction(casterStats, Action, castPosition, Radius);
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
    public bool zoneTargetsEnemies = false;

    public CombatAction ToCombatAction() { 
        Debug.Log("Combat Action Activated!");
        return new CombatAction
        {
            actionName        = "Spell",
            targetFriendly    = targetFriendly,
            maxTargets        = maxTargets,
            ScaleEffectsWithUsersStats = false,
            healthChangePercent  = healthChange,
            knockbackPercent = knockback,
            ZoneTargetsEnemies = zoneTargetsEnemies,
            effectsOnHit      = effectsOnHit,
            zoneData          = zoneData,
            zoneSpawnPosition = zoneMode == SpellZoneMode.AroundCastPoint ? ZoneSpawnPosition.Self
                            : zoneMode == SpellZoneMode.OnEachTarget   ? ZoneSpawnPosition.Touch
                            : ZoneSpawnPosition.Self,
            zoneSticky        = zoneSticky,
            excludeCasterFromZone = excludeCasterFromZone,
        };
    }
}

public enum SpellZoneMode { None, AroundCastPoint, OnEachTarget }