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
    bool _isOneShot; // is this really needed? just track that the lifespan is 0
    GameObject zoneVisuals;
    HashSet<Stats> _alreadyHit = new HashSet<Stats>(); // why HashSet why not just list?
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
     void Start()
    {
        if (data.zoneVisualPrefab != null) zoneVisuals = Instantiate(data.zoneVisualPrefab, transform);
        zoneVisuals.SetActive(false);
        if (!_initialized && data != null) ActivateField();
    }

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

        EenableZoneVisuals();

        if (_isOneShot) ApplyEffectsToTargets();
    }
    void EenableZoneVisuals()
    {
        // other effects
        zoneVisuals.SetActive(true);
    }

    public void DeactivateField()
    {
        _initialized = false;
        DisableZoneVisuals();
    }

    void DisableZoneVisuals()
    {
        zoneVisuals.SetActive(false);
    }

    void Update()
    {
        if (!_initialized || _isOneShot) return;
        CheckStickToTarget();
        UpdateRefreshTimer();
        ItterateLifeSpan();
        
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

        if (_isOneShot) Destroy(gameObject);
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
        if (data.effects == null || data.effects.Length == 0) return;

        if (data.applicationMode == ZoneApplicationMode.All) 
        {
            foreach (var effect in data.effects) target.statusEffectManager.ApplyEffect(effect, effect.duration);
        }
        else
        {
            StatusEffect chosen = data.effects[Random.Range(0, data.effects.Length)];
            target.statusEffectManager.ApplyEffect(chosen, chosen.duration);
        }
    }
    void ItterateLifeSpan()
    {
        if (data.zoneLifespan > 0f)
        {
            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= data.zoneLifespan)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (data == null) return;

        Gizmos.color = data.gizmoColor;

        if (data.shape == ZoneShape.Circle)
            Gizmos.DrawSphere(transform.position, data.circleRadius);
        else
            Gizmos.DrawCube(transform.position, new Vector3(data.boxSize.x, data.boxSize.y, 0.1f));
    }

    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        List<string> debugTags = _targetTagsOverride ?? targetTags;
        string tagStr = debugTags != null && debugTags.Count > 0 ? string.Join(",", debugTags) : "NONE";

        Vector3 labelPos = transform.position + Vector3.up * (data.shape == ZoneShape.Circle ? data.circleRadius : data.boxSize.y * 0.5f) + Vector3.up * 0.5f;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, $"[{data.name}]\nTags: {tagStr}\nExcludeCaster: {_excludeCaster}\nSticky: {_isSticky}\nRefresh: {data.refreshInterval}s");
#endif
    }
}