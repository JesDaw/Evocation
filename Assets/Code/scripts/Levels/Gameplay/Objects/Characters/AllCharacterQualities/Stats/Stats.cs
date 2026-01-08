using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Stats : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Assign ScriptableStats here to configure this unit")]
    public ScriptableStats scriptableStats;

    [Header("Clan & Targeting")]
    [Tooltip("What tags should this unit target? In priority order (first = highest priority)")]
    public List<string> targetTags = new List<string>();
    
    [Tooltip("Is this an enemy unit? If false, it's Player/Ally")]
    public bool _Enemy;

    

    [Header("Health")]
    [HideInInspector] public int _MaxHealth = 1;
    [HideInInspector] public float _CurrentHealth = 1;

    [Header("Attack")]
    [HideInInspector] public int _AttackDamage;
    [HideInInspector] public float _AttackEndlag;
    [HideInInspector] public Vector2 _AttackRange;

    [Header("Movement")]
    [HideInInspector] public float _MoveSpeed;

    [Header("Knockback")]
    [HideInInspector] public float _KnockBackMax;
    [HideInInspector] public float _KnockBackHealth;

    [Header("Spawn")]
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
        damageHandler = GetComponent<DamageHandler>();
        if (damageHandler == null)
            damageHandler = gameObject.AddComponent<DamageHandler>();

        statusEffectManager = GetComponent<StatusEffectManager>();
        if (statusEffectManager == null)
            statusEffectManager = gameObject.AddComponent<StatusEffectManager>();

        damageHandler.Initialize(this);
        statusEffectManager.Initialize(this);
    }

    public void InitializeStats()
    {
        SetupTag();
        
        SetupTargetingPriorities();
        
        InitializeFromScriptableStats();
        
        OnStatsInitialized?.Invoke();
        
        //Debug.Log($"{gameObject.name} stats initialized. Tag: {gameObject.tag}");
    }


    void SetupTag()
    {
        if (_Enemy)
        {
            gameObject.tag = "Enemy";
        }
        else
        {
            gameObject.tag = "Allies"; 
        }
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

    void InitializeFromScriptableStats()
    {
        if (scriptableStats == null) return;

        _MaxHealth = scriptableStats._MaxHealth;
        _CurrentHealth = scriptableStats._MaxHealth;
        _MoveSpeed = scriptableStats._MoveSpeed;
        _KnockBackMax = scriptableStats._KnockBackMax;
        _KnockBackHealth = scriptableStats._KnockBackMax;
        _spawnCost = scriptableStats._spawnCost;
        
        _AttackDamage = scriptableStats._AttackDamage;
        _AttackEndlag = scriptableStats._AttackEndlag; // Updated name
        _AttackRange = new Vector2(scriptableStats._HorizontalRange, scriptableStats._VerticalRange);
        
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
}