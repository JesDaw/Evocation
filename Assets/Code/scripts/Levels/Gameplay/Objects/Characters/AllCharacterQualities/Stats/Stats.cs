using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Stats : MonoBehaviour
{
    [Header("Configuration")]
    public ScriptableStats scriptableStats;

    [Header("Clan & Targeting")]
    public List<string> targetTags = new List<string>();
    public bool _Enemy;
    [SerializeField] bool player = false;

    [Header("Runtime Combat")]
    [HideInInspector] public int _AttackDamage;
    [HideInInspector] public float _AttackEndlag;
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
    [HideInInspector] public float _MoveSpeed;
    [HideInInspector] public float _KnockBackMax;
    [HideInInspector] public float _KnockBackHealth;
    [HideInInspector] public int _spawnCost;

    [Header("Events")]
    [SerializeField] internal UltEvents.UltEvent OnDeath, OnDamage, OnKnocked;
    [SerializeField] internal UltEvents.UltEvent<bool> OnWitFlagDeath, OnWitFlagDamage;
    [SerializeField] public UnityEvent OnStatsInitialized;

    [Header("Settings")]
    [SerializeField] bool _Invincible = false;
    public bool _DontDestroy = false;

    [HideInInspector] public DamageHandler damageHandler;
    [HideInInspector] public StatusEffectManager statusEffectManager;

    public DamageSource LastHitBy { get; set; }

    void Awake()
    {
        damageHandler = GetComponent<DamageHandler>() ?? gameObject.AddComponent<DamageHandler>();
        statusEffectManager = GetComponent<StatusEffectManager>() ?? gameObject.AddComponent<StatusEffectManager>();
        damageHandler.Initialize(this);
        statusEffectManager.Initialize(this);
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
        if (scriptableStats == null) return;

        _MaxHealth = scriptableStats._MaxHealth;
        _CurrentHealth = _MaxHealth;
        _MoveSpeed = scriptableStats._MoveSpeed;
        _KnockBackMax = scriptableStats._KnockBackMax;
        _KnockBackHealth = _KnockBackMax;
        _spawnCost = scriptableStats._spawnCost;
        
        _AttackDamage = scriptableStats._AttackDamage;
        _AttackEndlag = scriptableStats._AttackEndlag;
        _AttackRange = new Vector2(scriptableStats._HorizontalRange, scriptableStats._VerticalRange);
        
        _IsProjectile = scriptableStats._AttackStyle == AttackStyle.Projectile;
        _IsAOE = scriptableStats._IsAOE;
        _MaxAOETargets = scriptableStats._MaxAOETargets;
        _EffectsToApply = scriptableStats._EffectsToApply;

        if (_IsProjectile)
        {
            _ProjectilePrefab = scriptableStats._ProjectilePrefab;
            _ProjectileSpeed = scriptableStats._ProjectileSpeed;
            _ProjectileMaxHeight = scriptableStats._ProjectileMaxHeight;
            _TrajectoryCurve = scriptableStats._TrajectoryCurve;
            _AxisCorrectionCurve = scriptableStats._AxisCorrectionCurve;
            _SpeedCurve = scriptableStats._SpeedCurve;
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

    public void TakeDamage(float damage, DamageSource attackedBy = null)
    {
        if (damageHandler != null)
        {
            damageHandler.TakeDamage(damage, attackedBy);
        }
    }

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