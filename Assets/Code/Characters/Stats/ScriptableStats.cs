using System.Collections.Generic;
using UnityEngine;
using static System.MathF;
[CreateAssetMenu(fileName = "CpuStats", menuName = "CPU/Stats", order = 0)]
public class ScriptableStats : ScriptableObject
{
    [Header("Personality & Role")]
    public string Theme;
    public string ODS;
    public string RPS_Type;
    [TextArea(2, 5)] public string OtherNotes;
    public string SpawnSoundName = "";

    [Header("Value")]
    public int _spawnCost;
    public float _spawnCooldown;
    public float Level_Total;
    [HideInInspector] public float Level_Discrepancy;
    public float _CalculatedPower;
    [HideInInspector] public float _ValueDiscrepancy;
    [Header("Level Breakdown")]
    [Tooltip("AttackDamage + KnockbackDamage")] public float Attack;
    [Tooltip("MaxHealth + KnockbackMaxHealth")] public float Defense;
    [Tooltip("HorizontalRange/K_Range + MoveSpeed/K_MoveSpeed")] public float SpaceControl;
    [Tooltip("K_AttackRate*1000 / (Startup + Recovery + extraEndlag)")] public float AttackFrequency;

    [Header("Attack Power")]
    [Range(0, 1000)] public int _AttackDamage;
    [Range(0, 1000)] public float _KnockBackDamage;
    [Range(0, 15)] public float _MoveSpeed;
    [Header("Defensive Power")]
    [Range(0, 1000)]public int _MaxHealth = 1;
    [Range(0, 1000)]public float _KnockBackMaxHealth = 1;
    [Range(0, 30)] public float _HorizontalRange = 3f;
    public float _VerticalRange = 2f;
    [Header("Attack frequency")]
    [Tooltip("Extra endlag in seconds (animation recovery time is added on top)")] [Range(0, 30)]public float _ExtraEndlag = 0f;
    [Tooltip("Base multiplier for action cooldowns (1.0 = normal speed)")] [Range(1, 30)] public float _ActionCooldown = 1f;
    [Header("Knockback Physics")]
    public float _KnockBackVelocity = 10f;
    public float _KnockBackAngle = 45f;

    [Header("Combat Actions")]
    public List<CombatAction> combatActions = new List<CombatAction>();

    [Header("Visuals & Animation")]
    public bool _Rotate;
    public AnimatorOverrideController _animator;
    public animationRigs[] _Sprites;
    [Range(0, 10)] public float _AnimationMoveSpeed = 1f;
    public List<GameObject> vfx = new();
    public List<Vector2> vfxOffsets = new();
    [Tooltip("in ms")] public float _AnimationStartupTime;
    [Tooltip("in ms")] public float _AnimationRecoveryTime;

    [Header("Level")]
    public int level = 1;
    [SerializeField] float xp_to_next_lvl = 100;
    [SerializeField] float ExpCostMultiplier = .5f;

    public float TryLevelUp(float xp)
    {
        if (xp <= xp_to_next_lvl)
        {
            ChangeLevel();
            return xp - xp_to_next_lvl;
        }
        else
        {
            Debug.Log($"need {xp_to_next_lvl - xp} mroe exp");
            return xp;
        }
    }
    void ChangeLevel()
    {
        level += 1;
        xp_to_next_lvl *= Mathf.Pow((1+ExpCostMultiplier), level);
    }

#if UNITY_EDITOR
    private static BalanceConfig _cachedConfig;

    void OnValidate()
    {
        // Prevent AssetDatabase calls during the asset import / refresh loop
        if (UnityEditor.EditorApplication.isUpdating || UnityEditor.BuildPipeline.isBuildingPlayer)
            return;

        // Defer execution until after OnValidate and serialization have finished
        UnityEditor.EditorApplication.delayCall -= DelayedValidate;
        UnityEditor.EditorApplication.delayCall += DelayedValidate;
    }

    private void DelayedValidate()
    {
        // Ensure the object wasn't destroyed before the delayed call fired
        if (this == null || UnityEditor.EditorApplication.isUpdating)
            return;

        // 1. Try to get config from cached reference or AssetDatabase
        if (_cachedConfig == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BalanceConfig");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                _cachedConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<BalanceConfig>(path);
            }
        }

        ScriptableStats anchor = _cachedConfig != null ? _cachedConfig.anchorUnit : null;
        BalanceSimulationRunner.CobbDouglasElasticities el = _cachedConfig != null 
            ? _cachedConfig.elasticities 
            : default;

        // If no BalanceConfig asset exists yet, check the scene runner
        if (anchor == null)
        {
            var runner = FindFirstObjectByType<BalanceSimulationRunner>();
            if (runner != null)
            {
                anchor = runner.anchorUnit;
                el = runner.elasticities;
            }
        }

        // Safety fallback: ensure scaleConstant is never 0
        if (el.scaleConstant <= 0f) el.scaleConstant = 1.0f;
        if (el.e_Health <= 0f && el.e_DPS <= 0f)
        {
            el.e_Health = 0.8f;
            el.e_DPS = 1.0f;
            el.e_Range = 0.5f;
            el.e_MoveSpeed = 0.3f;
        }

        if (anchor != null && anchor != this)
        {
            float anchorCost = anchor._spawnCost > 0 ? anchor._spawnCost : 50f;
            float newPower = BalanceSimulationRunner.EvaluateEstimatedPower(this, anchor, el, anchorCost);
            float newDiscrepancy = newPower - _spawnCost;

            // Only dirty the asset if values actually changed
            if (!Mathf.Approximately(_CalculatedPower, newPower) || !Mathf.Approximately(_ValueDiscrepancy, newDiscrepancy))
            {
                _CalculatedPower = newPower;
                _ValueDiscrepancy = newDiscrepancy;
                Level_Total = _CalculatedPower;
                Level_Discrepancy = _ValueDiscrepancy;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
    }
#endif

}

[System.Serializable]
public class animationRigs
{
    public enum animationKey { Idle, Running, Attack, Knockback }
    public animationKey Key;
    public GameObject Rig;
    public Vector2 Offset;
}
