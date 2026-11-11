using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Briefly tints every SpriteRenderer on the entity when it takes damage.
/// Works the same way as the spawn tint in SpawnObjects (writes SpriteRenderer.color), but remembers each
/// renderer's color before the flash and puts it back afterwards, so team tints survive.
/// Only RGB is touched: alpha is left alone so fades that run during a flash still work.
/// Added automatically by DamageHandler; add it to a prefab manually to tweak the values per entity.
/// </summary>
public class DamageFlash : MonoBehaviour
{
    [SerializeField] Color flashColor = new Color(1f, 0.15f, 0.15f);
    [Tooltip("How far the sprites are pulled toward Flash Color at the start of the flash. 0 = off, 1 = solid flash color.")]
    [Range(0f, 1f)] [SerializeField] float flashStrength = 0.7f;
    [Tooltip("Seconds for the flash to fade back to the normal color.")]
    [Min(0.01f)] [SerializeField] float flashDuration = 0.15f;

    readonly Dictionary<SpriteRenderer, Color> baseColors = new();
    readonly List<SpriteRenderer> renderers = new();
    Coroutine flashRoutine;

    public void Flash()
    {
        if (flashStrength <= 0f || !isActiveAndEnabled) return;

        // Re-collect every hit: CPU rigs are built after spawning, so the renderer set can change.
        // Renderers already mid-flash keep the base color cached on the first hit.
        GetComponentsInChildren(true, renderers);
        foreach (var sr in renderers)
        {
            if (!baseColors.ContainsKey(sr)) baseColors[sr] = sr.color;
        }

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < flashDuration)
        {
            Apply(flashStrength * (1f - t / flashDuration));
            yield return null;
            t += Time.deltaTime;
        }

        Restore();
        flashRoutine = null;
    }

    void Apply(float amount)
    {
        foreach (var pair in baseColors)
        {
            var sr = pair.Key;
            if (sr == null) continue;
            Color b = pair.Value;
            sr.color = new Color(
                Mathf.Lerp(b.r, flashColor.r, amount),
                Mathf.Lerp(b.g, flashColor.g, amount),
                Mathf.Lerp(b.b, flashColor.b, amount),
                sr.color.a);
        }
    }

    void Restore()
    {
        foreach (var pair in baseColors)
        {
            var sr = pair.Key;
            if (sr == null) continue;
            Color b = pair.Value;
            sr.color = new Color(b.r, b.g, b.b, sr.color.a);
        }
        baseColors.Clear();
    }

    void OnDisable()
    {
        // A coroutine stops when the object is disabled, so don't leave sprites stuck mid-flash.
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        Restore();
    }
}
