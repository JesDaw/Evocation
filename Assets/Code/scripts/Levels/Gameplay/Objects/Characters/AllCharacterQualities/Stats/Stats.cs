using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Stats : MonoBehaviour, IDamageable
{
    [Header("Configuration")]
    public ScriptableStats scriptableStats;

    [Header("Clan & Targeting")]
    public List<string> targetTags = new List<string>();
    public bool _Enemy;
    [SerializeField] bool player = false;

    [Header("Runtime Combat")]
    [HideInInspector] public int _AttackDamage;
    [HideInInspector] public float _ExtraEndlag;
    [HideInInspector] public Vector2 _AttackRange;
    [HideInInspector] public bool _IsProjectile; 
    [HideInInspector] public bool _IsAOE;
    [HideInInspector] public int _MaxAOETargets;
    
    [HideInInspector] public GameObject _ProjectilePrefab;
    [HideInInspector] public float _ProjectileSpeed;
    [HideInInspector] public float _ProjectileMaxHeight;
    [HideInInspector] public AnimationCurve _TrajectoryCurve;
    [HideInInspector] public AnimationCurve _AxisCorrectionCurve;
    [HideInInspector] public AnimationCurve _SpeedCurve;
    [HideInInspector] public List<StatusEffect> _EffectsToApply;

    [Header("Health & Movement")]
    [HideInInspector] public int _MaxHealth = 1;
    [HideInInspector] public float _CurrentHealth = 1;
    [HideInInspector] public bool _IsDead = false;
    [HideInInspector] public float _MoveSpeed;
    [HideInInspector] public float _AnimationStartupTime;
    [HideInInspector] public float _AnimationRecoveryTime;
    public float _KnockBackMaxHealth;
    public float _KnockBackHealth;
    public float _KnockBackDamage = 0.5f;
    [HideInInspector] public int _spawnCost;

    [Header("Events")]
    [SerializeField] internal UltEvents.UltEvent OnDeath, OnDamage, OnKnocked;
    [SerializeField] internal UltEvents.UltEvent<bool> OnWitFlagDeath, OnWitFlagDamage;
    [SerializeField] public UnityEvent OnStatsInitialized;
    public UnityEvent DamageTrigger;
    public float DamageTriggerAmount = -100.0f;

    [Header("Settings")]
    [SerializeField] bool _Invincible = false;
    public bool _DontDestroy = false;

    [HideInInspector] public DamageHandler damageHandler;
    [HideInInspector] public StatusEffectManager statusEffectManager;
    [HideInInspector] public EntityHealthbar entityHealthbar;

    public DamageSource LastHitBy { get; set; }

    void Awake()
    {
        damageHandler = GetComponent<DamageHandler>();
        if (damageHandler == null)
        {
            damageHandler = gameObject.AddComponent<DamageHandler>();
        }
        statusEffectManager = GetComponent<StatusEffectManager>();
        if (statusEffectManager == null)
        {
            statusEffectManager = gameObject.AddComponent<StatusEffectManager>();
        }
        if (entityHealthbar == null)
        {
            entityHealthbar = GetComponent<EntityHealthbar>();
        }
        if (damageHandler != null)
        {
            damageHandler.Initialize(this);
        }
        else
        {
            Debug.LogError($"{gameObject.name}: Failed to get or add DamageHandler component.");
        }
        if (statusEffectManager != null)
        {
            statusEffectManager.Initialize(this);
        }
        else
        {
            Debug.LogError($"{gameObject.name}: Failed to get or add StatusEffectManager component.");
        }
        if (entityHealthbar != null)
        {
            entityHealthbar.Initialize(this);
        }
        
    }

    public void InitializeStats()
    {
        SetupTag();
        SetupTargetingPriorities();
        InitializeFromScriptableStats();
        OnStatsInitialized?.Invoke();
    }

    void InitializeFromScriptableStats()
    {
        if (scriptableStats == null)
        {
            Debug.LogWarning($"{gameObject.name}: scriptableStats is null, cannot initialize.");
            return;
        }

        _MaxHealth = scriptableStats._MaxHealth;
        if (_MaxHealth <= 0) Debug.LogWarning($"{gameObject.name}: MaxHealth is {_MaxHealth}, should be positive.");

        _CurrentHealth = _MaxHealth;
        _MoveSpeed = scriptableStats._MoveSpeed;
        if (_MoveSpeed < 0) Debug.LogWarning($"{gameObject.name}: MoveSpeed is {_MoveSpeed}, should be non-negative.");

        _KnockBackMaxHealth = scriptableStats._KnockBackMaxHealth;
        _KnockBackDamage = scriptableStats._KnockBackDamage;
        _KnockBackHealth = _KnockBackMaxHealth;
        _spawnCost = scriptableStats._spawnCost;
        if (_spawnCost < 0) Debug.LogWarning($"{gameObject.name}: SpawnCost is {_spawnCost}, should be non-negative.");

        _AttackDamage = scriptableStats._AttackDamage;
        if (_AttackDamage < 0) Debug.LogWarning($"{gameObject.name}: AttackDamage is {_AttackDamage}, should be non-negative.");

        _ExtraEndlag = scriptableStats._ExtraEndlag;
        _AnimationStartupTime = scriptableStats._AnimationStartupTime;
        _AnimationRecoveryTime = scriptableStats._AnimationRecoveryTime;
        _AttackRange = new Vector2(scriptableStats._HorizontalRange, scriptableStats._VerticalRange);

        _IsProjectile = scriptableStats._AttackStyle == AttackStyle.Projectile;
        _IsAOE = scriptableStats._IsAOE;
        _MaxAOETargets = scriptableStats._MaxAOETargets;
        _EffectsToApply = scriptableStats._EffectsToApply;

        if (_IsProjectile)
        {
            _ProjectilePrefab = scriptableStats._ProjectilePrefab;
            if (_ProjectilePrefab == null) Debug.LogWarning($"{gameObject.name}: Projectile prefab is null.");

            _ProjectileSpeed = scriptableStats._ProjectileSpeed;
            _ProjectileMaxHeight = scriptableStats._ProjectileMaxHeight;
            _TrajectoryCurve = scriptableStats._TrajectoryCurve;
            if (_TrajectoryCurve == null) Debug.LogWarning($"{gameObject.name}: Trajectory curve is null.");

            _AxisCorrectionCurve = scriptableStats._AxisCorrectionCurve;
            if (_AxisCorrectionCurve == null) Debug.LogWarning($"{gameObject.name}: Axis correction curve is null.");

            _SpeedCurve = scriptableStats._SpeedCurve;
            if (_SpeedCurve == null) Debug.LogWarning($"{gameObject.name}: Speed curve is null.");
        }
    }

    public void SetTag(string tag)
    {
        gameObject.tag = tag;
    }

    public void AddTargetTag(string tag)
    {
        if (!targetTags.Contains(tag))
        {
            targetTags.Add(tag);
        }
    }

    public void AddStatusEffect(StatusEffect effect)
    {
        if (statusEffectManager != null)
        {
            statusEffectManager.AddEffect(effect);
        }
    }

    public void TakeDamage(float damage, float knockback_damage, DamageSource attackedBy = null)
    {
        if (damageHandler != null)
        {
            damageHandler.TakeDamage(damage, knockback_damage, attackedBy);
        }
    }

    public void TakeDamage(float damage, DamageSource attackedBy = null)
    {
        if (damageHandler != null)
        {
            damageHandler.TakeDamage(damage, attackedBy);
        }
    }

    void IDamageable.TakeDamage(float damage, float knockback_damage, DamageSource source)
    {
        TakeDamage(damage, knockback_damage, source);
    }
    
    void IDamageable.TakeDamage(float damage, DamageSource source)
    {
        TakeDamage(damage, source);
    }


    GameObject IDamageable.gameObject => gameObject;
    Transform IDamageable.transform => transform;

    public void ToggleInvincibility() 
    { 
        _Invincible = !_Invincible; 
    }

    public bool IsInvincible() 
    { 
        return _Invincible; 
    }

    public void SetHealth(float amount)
    {
        _CurrentHealth = Mathf.Clamp(amount, 0, _MaxHealth);
    }

    public void SetDestroyed(bool shouldDestroy)
    {
        _DontDestroy = shouldDestroy;
    }

    void SetupTag()
    {
        if (_Enemy) gameObject.tag = "Enemy";
        else if(player) gameObject.tag = "Player"; 
        else gameObject.tag = "Allies";
    }

    void SetupTargetingPriorities()
    {
        if (targetTags.Count > 0) return;
        if (_Enemy)
        {
            targetTags.Add("Player");
            targetTags.Add("Allies");
        }
        else
        {
            targetTags.Add("Enemy");
        }
    }
}
