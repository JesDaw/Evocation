using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Fireball", fileName = "New Fireball Spell")]
public class FireballSpell : SpellDefinition
{
    [Header("Hit Resolution")]
    [Tooltip("When the hit actually resolves. Independent of visual timing below.")]
    [SerializeField] float hitResolveTime = 0.3f;
    [SerializeField] float cameraShakeForce = 1f;
    [SerializeField] float audioDelay;
    [SerializeField] float vfxDelay;

    [Header("Vignette Opacity")]
    public TimedCurve vignetteOpacityIn = new TimedCurve { startTime = 0f, duration = 0.3f, curve = AnimationCurve.EaseInOut(0, 0f, 1, 1f) };
    public TimedCurve vignetteOpacityOut = new TimedCurve { startTime = 0.3f, duration = 0.5f, curve = AnimationCurve.EaseInOut(0, 1f, 1, 0f) };

    [Header("Screen Crack Opacity")]
    public TimedCurve screenCrackOpacityIn = new TimedCurve { startTime = 0.15f, duration = 0.15f, curve = AnimationCurve.EaseInOut(0, 0f, 1, 1f) };
    public TimedCurve screenCrackOpacityOut = new TimedCurve { startTime = 0.3f, duration = 0.5f, curve = AnimationCurve.EaseInOut(0, 1f, 1, 0f) };

    [Header("Time Scale")]
    public TimedCurve timeScaleIn = new TimedCurve { startTime = 0f, duration = 0.15f, curve = AnimationCurve.EaseInOut(0, 1f, 1, 0.2f) };
    public TimedCurve timeScaleOut = new TimedCurve { startTime = 0.3f, duration = 0.4f, curve = AnimationCurve.EaseInOut(0, 0.2f, 1, 1f) };

    static readonly Color StartVignetteColor = Color.black;
    static readonly int VignetteStrengthID = Shader.PropertyToID("_VignetteStrength");
    static readonly int CrackStrengthID = Shader.PropertyToID("_ScreenCrackOpacity");
    static readonly int VignetteColorID = Shader.PropertyToID("_VignetteColor");
    bool vfxSpawned = false;
    bool audioPlayed = false;
    GameObject vfx;

    public override IEnumerator RunCastSequence(SpellCaster caster, Vector3 castPosition)
    {
        Shader.SetGlobalColor(VignetteColorID, StartVignetteColor);

        bool hitResolved = false;

        try
        {
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float vignetteOpacity = elapsed < hitResolveTime 
                    ? vignetteOpacityIn.Evaluate(elapsed) 
                    : vignetteOpacityOut.Evaluate(elapsed);
                Shader.SetGlobalFloat(VignetteStrengthID, vignetteOpacity);

                float crackOpacity = elapsed < hitResolveTime 
                    ? screenCrackOpacityIn.Evaluate(elapsed) 
                    : screenCrackOpacityOut.Evaluate(elapsed);
                Shader.SetGlobalFloat(CrackStrengthID, crackOpacity);

                float timeScale = elapsed < hitResolveTime 
                    ? timeScaleIn.Evaluate(elapsed) 
                    : timeScaleOut.Evaluate(elapsed);
                UILogic.RequestSpellTimeScale(timeScale);

                if (!vfxSpawned && elapsed >= vfxDelay) 
                {
                    vfx = spellVFX != null ? Instantiate(spellVFX, castPosition, Quaternion.identity) : null;
                    vfxSpawned = true;
                }
                if (!audioPlayed && elapsed >= audioDelay) 
                {
                    FModAudioManager.instance.PlaySoundByName(castSoundName);
                    audioPlayed = true;
                }

                if (!hitResolved && elapsed >= hitResolveTime)
                {
                    ResolveHit(caster, castPosition);
                    CameraEffects.Instance.Shake(cameraShakeForce);
                    hitResolved = true;
                }

                yield return null;
            }

            if (!hitResolved) ResolveHit(caster, castPosition);
        }
        finally
        {
            UILogic.ClearSpellTimeScale();
            Shader.SetGlobalColor(VignetteColorID, StartVignetteColor);
            Shader.SetGlobalFloat(VignetteStrengthID, 0f);
            Shader.SetGlobalFloat(CrackStrengthID, 0f);
            if (vfx != null) Destroy(vfx);
            vfxSpawned = false;
            audioPlayed = false;
        }
    }
}



/// <summary>
/// A value that ramps along an AnimationCurve over [startTime, startTime+duration],
/// evaluated against a shared elapsed-time clock. Holds flat at curve(0) before
/// startTime and curve(1) after it ends.
/// </summary>
[System.Serializable]
public struct TimedCurve
{
    public float startTime;
    public float duration;
    public AnimationCurve curve;

    public float Evaluate(float elapsed)
    {
        if (duration <= 0f) return curve.Evaluate(elapsed >= startTime ? 1f : 0f);
        float t = Mathf.Clamp01((elapsed - startTime) / duration);
        return curve.Evaluate(t);
    }
}