using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TEMPORARY placeholder until final character art is in: tints characters by side so teams are easy to tell apart.
///   Allies = green, enemies = red, your hero = blue.
///
/// Nothing else references this. It starts itself in every scene, so to remove it just delete this file
/// (and its .meta). To switch it off without deleting, set Enabled to false.
/// </summary>
public class TemporaryTeamTint : MonoBehaviour
{
    const bool Enabled = true;

    // Multiplied with each sprite's own color, so the art still shows through.
    static readonly Color AllyTint  = new Color(0.7f, 1f, 0.7f);
    static readonly Color EnemyTint = new Color(1f, 0.66f, 0.66f);
    static readonly Color HeroTint  = new Color(0.7f, 0.83f, 1f);

    // Characters swap their animation rigs at runtime, so new sprite pieces appear after spawn. Re-check a few times a second.
    const float RefreshInterval = 0.25f;

    // Each sprite's color before tinting, so the tint is applied once rather than stacking every refresh.
    readonly Dictionary<SpriteRenderer, Color> _originalColors = new();
    readonly List<SpriteRenderer> _renderers = new();
    float _nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
#pragma warning disable CS0162 // unreachable when disabled
        if (!Enabled) return;
#pragma warning restore CS0162
        var go = new GameObject("[Temporary Team Tint]");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<TemporaryTeamTint>();
    }

    void LateUpdate()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + RefreshInterval;

        foreach (var stats in FindObjectsByType<Stats>(FindObjectsSortMode.None))
        {
            if (!TryGetTint(stats, out Color tint)) continue;

            stats.GetComponentsInChildren(true, _renderers);
            foreach (var sr in _renderers)
            {
                if (!_originalColors.TryGetValue(sr, out Color original))
                {
                    original = sr.color;
                    _originalColors[sr] = original;
                }

                // Keep the current alpha: other effects fade characters in and out.
                sr.color = new Color(original.r * tint.r, original.g * tint.g, original.b * tint.b, sr.color.a);
            }
        }

        // Forget destroyed sprites so the cache doesn't grow forever.
        if (_originalColors.Count > 2000)
        {
            var dead = new List<SpriteRenderer>();
            foreach (var sr in _originalColors.Keys) if (sr == null) dead.Add(sr);
            foreach (var sr in dead) _originalColors.Remove(sr);
        }
    }

    static bool TryGetTint(Stats stats, out Color tint)
    {
        if (stats.GetComponent<PlayerStateMachine>() != null)
        {
            tint = HeroTint;
            return true;
        }
        // Only units: skip bases/buildings, projectiles and anything else with Stats.
        if (stats.GetComponent<CpuStateManager>() != null)
        {
            tint = stats._Enemy ? EnemyTint : AllyTint;
            return true;
        }
        tint = Color.white;
        return false;
    }
}
