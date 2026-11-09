using System.Collections.Generic;
using UnityEngine;

public class AreaEffectZone : MonoBehaviour
{
    public AreaEffectData data;

    [Header("Targeting")]
    public List<string> targetTags = new List<string>();
    Stats _caster;
    bool _excludeCaster;
    bool _isSticky;
    Transform _stickyTarget;
    public bool IsSticky => _isSticky;
    List<string> _targetTagsOverride;
    float _lifeTimer;
    float _refreshTimer;
    bool _initialized;
    bool _isOneShot; // lifespan can't replace this: one-shot is driven by refreshInterval <= 0
    GameObject zoneVisualObj;
    ZoneVisual zoneVisual;
    HashSet<Stats> _alreadyHit = new HashSet<Stats>();

    public void Initialize(AreaEffectData areaEffectData, Stats caster, Transform stickyTarget, bool excludeCaster, bool? stickyOverride = null, List<string> targetTagsOverride = null)
    {
        data = areaEffectData;
        _caster = caster;
        _stickyTarget = stickyTarget;
        _excludeCaster = excludeCaster;
        _isSticky = stickyOverride ?? (data != null && data.sticky);
        _targetTagsOverride = targetTagsOverride;
        ActivateField();
    }

    // Fallback for zones placed in a scene by hand with data assigned in the inspector
    void Start()
    {
        if (!_initialized && data != null) ActivateField();
    }

    #region activation and deactivation
    public void ActivateField()
    {
        if (data == null)
        {
            Debug.LogWarning($"AreaEffectZone on {gameObject.name}: data is null, zone will not function.");
            return;
        }

        _isOneShot = data.refreshInterval <= 0f;
        _lifeTimer = 0f;
        _refreshTimer = 0f;
        _initialized = true;

        EnableZoneVisuals();

        if (_isOneShot) ApplyEffectsToTargets();
    }

    void EnableZoneVisuals()
    {
        // Spawn once, then reuse on re-activation
        if (zoneVisualObj == null && data.zoneVisualPrefab != null)
        {
            zoneVisualObj = Instantiate(data.zoneVisualPrefab, transform);
            zoneVisualObj.transform.localPosition = Vector3.zero;
            zoneVisual = zoneVisualObj.GetComponent<ZoneVisual>();

            if (zoneVisual == null)
                Debug.LogWarning($"AreaEffectZone on {gameObject.name}: zoneVisualPrefab has no ZoneVisual component on its root, so size/colors won't be applied.");
        }

        if (zoneVisual != null)
        {
            zoneVisual.Setup(data);
            zoneVisual.Show();
        }
        else if (zoneVisualObj != null)
        {
            zoneVisualObj.SetActive(true);
        }
    }

    public void DeactivateField()
    {
        _initialized = false;
        DisableZoneVisuals();
    }

    void DisableZoneVisuals()
    {
        if (zoneVisualObj != null) zoneVisualObj.SetActive(false);
    }
    #endregion

    #region loop when the zone is active
    void Update()
    {
        if (!_initialized || _isOneShot) return;
        CheckStickToTarget();
        UpdateRefreshTimer();
        IterateLifeSpan();
    }

    void CheckStickToTarget()
    {
        if (_isSticky && _stickyTarget != null) transform.position = _stickyTarget.position;
    }

    void UpdateRefreshTimer()
    {
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= data.refreshInterval)
        {
            _refreshTimer = 0f;
            ApplyEffectsToTargets();
        }
    }

    void ApplyEffectsToTargets()
    {
        List<Stats> targets = GatherTargets();

        int applied = 0;
        foreach (Stats target in targets)
        {
            if (data.maxTargets >= 0 && applied >= data.maxTargets) break;
            if (_isOneShot && _alreadyHit.Contains(target)) continue;

            ApplyEffectsTo(target);

            if (_isOneShot) _alreadyHit.Add(target);
            applied++;
        }

        if (_isOneShot)
        {
            _initialized = false;
            Destroy(gameObject, Mathf.Max(0f, data.oneShotVisualDuration));
        }
    }

    List<Stats> GatherTargets()
    {
        List<string> tagsToUse = _targetTagsOverride ?? targetTags;

        if (tagsToUse == null || tagsToUse.Count == 0)
        {
            Debug.LogError($"AreaEffectZone on {gameObject.name}: No target tags configured.");
            return new List<Stats>();
        }

        Stats casterFilter = _excludeCaster ? _caster : null;

        if (data.shape == ZoneShape.Circle)
        {
            return AttackDetection.FindTargetsInCircle(transform.position, data.circleRadius, tagsToUse, casterFilter);
        }
        else
        {
            return AttackDetection.FindTargetsInBox(transform.position, data.boxSize, tagsToUse, casterFilter);
        }
    }

    void ApplyEffectsTo(Stats target)
    {
        target.AlterHealth(data.HealthChangePerTick);
        if (data.effects == null || data.effects.Length == 0) return;

        if (data.applicationMode == ZoneApplicationMode.All)
        {
            foreach (var effect in data.effects) target.statusEffectManager.AddEffect(effect, effect.duration);
        }
        else
        {
            StatusEffect chosen = data.effects[Random.Range(0, data.effects.Length)];
            target.statusEffectManager.AddEffect(chosen, chosen.duration);
        }
    }

    void IterateLifeSpan()
    {
        if (data.zoneLifespan > 0f)
        {
            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= data.zoneLifespan)
            {
                Destroy(gameObject);
            }
        }
    }
    #endregion

    #region Debug
    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = new Color(data.gizmoColor.r, data.gizmoColor.g, data.gizmoColor.b, 1f);

        if (data.shape == ZoneShape.Circle)
            Gizmos.DrawWireSphere(transform.position, data.circleRadius);
        else
            Gizmos.DrawWireCube(transform.position, new Vector3(data.boxSize.x, data.boxSize.y, 0.1f));

        List<string> debugTags = _targetTagsOverride ?? targetTags;
        string tagStr = debugTags != null && debugTags.Count > 0 ? string.Join(",", debugTags) : "NONE";

        Vector3 labelPos = transform.position + Vector3.up * (data.shape == ZoneShape.Circle ? data.circleRadius : data.boxSize.y * 0.5f) + Vector3.up * 0.5f;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, $"[{data.name}]\nTags: {tagStr}\nExcludeCaster: {_excludeCaster}\nSticky: {_isSticky}\nRefresh: {data.refreshInterval}s");
#endif
    }
    #endregion
}