using System.Collections.Generic;
using UnityEngine;

public class AreaEffectZone : MonoBehaviour
{
    public AreaEffectData _data;

    Stats _caster;
    Transform _stickyTarget;
    bool _excludeCaster;
    bool _isSticky;

    float _lifeTimer;
    float _refreshTimer;
    bool _initialized;

    bool _isOneShot;
    HashSet<Stats> _alreadyHit = new HashSet<Stats>();

    public void Initialize(AreaEffectData data, Stats caster, Transform stickyTarget, bool excludeCaster, bool? stickyOverride = null)
    {
        _data = data;
        _caster = caster;
        _stickyTarget = stickyTarget;
        _excludeCaster = excludeCaster;
        _isSticky = stickyOverride ?? (data != null && data.sticky);

        Boot();
    }

    public void Boot()
    {
        if (_data == null)
        {
            Debug.LogWarning($"AreaEffectZone on {gameObject.name}: _data is null, zone will not function.");
            return;
        }

        _isOneShot = _data.refreshInterval <= 0f;
        _lifeTimer = 0f;
        _refreshTimer = 0f;
        _initialized = true;

        if (_data.zoneVisualPrefab != null)
            Instantiate(_data.zoneVisualPrefab, transform);

        if (_isOneShot)
        {
            ProcessOverlap();
        }
    }

    void Start()
    {
        if (!_initialized && _data != null)
            Boot();
    }

    void Update()
    {
        if (!_initialized || _isOneShot) return;

        if (_isSticky && _stickyTarget != null)
            transform.position = _stickyTarget.position;

        if (_data.zoneLifespan > 0f)
        {
            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= _data.zoneLifespan)
            {
                Destroy(gameObject);
                return;
            }
        }

        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= _data.refreshInterval)
        {
            _refreshTimer = 0f;
            ProcessOverlap();
        }
    }

    void ProcessOverlap()
    {
        List<Stats> targets = GatherTargets();

        int applied = 0;
        foreach (Stats target in targets)
        {
            if (_data.maxTargets >= 0 && applied >= _data.maxTargets) break;
            if (_isOneShot && _alreadyHit.Contains(target)) continue;

            ApplyEffectsTo(target);

            if (_isOneShot) _alreadyHit.Add(target);
            applied++;
        }

        if (_isOneShot) Destroy(gameObject);
    }

    List<Stats> GatherTargets()
    {
        Stats casterFilter = _excludeCaster ? _caster : null;

        if (_data.shape == ZoneShape.Circle)
        {
            return AttackDetection.FindTargetsInCircle(
                transform.position, _data.circleRadius, _data.targetTags, casterFilter);
        }
        else
        {
            List<IDamageable> raw = AttackDetection.FindTargetsInBox(
                transform.position, _data.boxSize, _data.targetTags, casterFilter);

            List<Stats> result = new List<Stats>(raw.Count);
            foreach (var d in raw)
                if (d is Stats s) result.Add(s);
            return result;
        }
    }

    void ApplyEffectsTo(Stats target)
    {
        if (_data.effects == null || _data.effects.Length == 0) return;

        if (_data.applicationMode == ZoneApplicationMode.All)
        {
            foreach (var effect in _data.effects)
                target.statusEffectManager.ApplyEffect(effect, effect.duration);
        }
        else
        {
            StatusEffect chosen = _data.effects[Random.Range(0, _data.effects.Length)];
            target.statusEffectManager.ApplyEffect(chosen, chosen.duration);
        }
    }

    void OnDrawGizmos()
    {
        if (_data == null) return;

        Gizmos.color = _data.gizmoColor;

        if (_data.shape == ZoneShape.Circle)
            Gizmos.DrawSphere(transform.position, _data.circleRadius);
        else
            Gizmos.DrawCube(transform.position, new Vector3(_data.boxSize.x, _data.boxSize.y, 0.1f));
    }
}