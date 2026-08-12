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
    [HideInInspector] public float _ActionCooldown;
    [HideInInspector] public Vector2 _AttackRange;
    [HideInInspector] public float _HorizontalRange;

    [HideInInspector] public bool _IsAOE;
    [HideInInspector] public int _MaxAOETargets;
    [HideInInspector] public GameObject _ProjectilePrefab;
    [HideInInspector] public float _ProjectileSpeed;
    [HideInInspector] public float _ProjectileMaxHeight;
    [HideInInspector] public AnimationCurve _TrajectoryCurve;
    [HideInInspector] public AnimationCurve _AxisCorrectionCurve;
    [HideInInspector] public AnimationCurve _SpeedCurve;
    [HideInInspector] public List<StatusEffect> _EffectsToApply;

    [HideInInspector] public bool _IsProjectile;

    [HideInInspector] public List<CombatAction> _CombatActions;
    [HideInInspector] public List<float> _ActionCooldownTimers;

    [HideInInspector] public Dictionary<StatusEffect, StaticEffectSnapshot> _EffectSnapshots
        = new Dictionary<StatusEffect, StaticEffectSnapshot>();

    [Header("Health & Movement")]
    [HideInInspector] public int _MaxHealth = 1;
    [HideInInspector] public float _CurrentHealth = 1;
    [HideInInspector] public bool _IsDead = false;
    [HideInInspector] public float _MoveSpeed;
    [HideInInspector] public float _CastSpeedMultiplier = 1f;
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
    [HideInInspector] public Animator animator;
    

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

        animator = GetComponentInChildren<Animator>();
    }

    void OnDestroy()
    {
        gameObject.SetActive(false);
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
            //Debug.LogWarning($"{gameObject.name}: scriptableStats is null, cannot initialize.");
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
        _ActionCooldown = scriptableStats._ActionCooldown;
        _AnimationStartupTime = scriptableStats._AnimationStartupTime;
        _AnimationRecoveryTime = scriptableStats._AnimationRecoveryTime;

        //Debug.Log($"[Stats] {gameObject.name} initialized: tag={tag}, targetTags=[{string.Join(",", targetTags)}], _Enemy={_Enemy}, _CastSpeedMultiplier={_CastSpeedMultiplier}");

        _HorizontalRange = scriptableStats._HorizontalRange;
        _AttackRange = new Vector2(scriptableStats._HorizontalRange, scriptableStats._VerticalRange);

        _CombatActions = new List<CombatAction>(scriptableStats.combatActions);
        _ActionCooldownTimers = new List<float>(new float[_CombatActions.Count]);
        _CastSpeedMultiplier = 1f;

        _IsAOE = false;
        _MaxAOETargets = 5;
        _EffectsToApply = new List<StatusEffect>();
        _IsProjectile = false;

        _ProjectilePrefab = null;
        _ProjectileSpeed = 15f;
        _ProjectileMaxHeight = 2f;
        _TrajectoryCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
        _AxisCorrectionCurve = AnimationCurve.Linear(0, 0, 1, 1);
        _SpeedCurve = AnimationCurve.Constant(0, 1, 1);
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

    public void AlterHealth(float amount, DamageSource source = null)
    {
        if (damageHandler != null)
        {
            if (amount < 0f)
            {
                damageHandler.TakeDamage(-amount, 0f, source ?? new DamageSource(DamageSource.DamageType.Melee));
            }
            else if (amount > 0f)
                damageHandler.Heal(amount);
        }
    }

    public void AlterKnockback(float amount, bool isEnemySource)
    {
        _KnockBackHealth += amount;

        if (_KnockBackHealth <= 0f)
        {
            OnKnocked?.Invoke();
            _KnockBackHealth = _KnockBackMaxHealth;
        }
    }

    //What is this stuff even for??==========
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

    //What is this stuff even for??==========

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

    public void TickActionCooldowns(float deltaTime)
    {
        float effectiveDelta = deltaTime * _CastSpeedMultiplier;
        for (int i = 0; i < _ActionCooldownTimers.Count; i++)
        {
            if (_ActionCooldownTimers[i] > 0f)
                _ActionCooldownTimers[i] -= effectiveDelta;
        }
    }
}

[System.Serializable]
public class StaticEffectSnapshot
{
    public float moveSpeed;
    public int attackDamage;
    public float knockbackDamage;
    public float horizontalRange;
    public float animationSpeed;
    public float castSpeedMultiplier;
    public int stackCount;
}