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

    [Header("Value")]
    public int _spawnCost;
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
    [Header("Sttack frequency")]
    [Tooltip("Extra endlag in seconds (animation recovery time is added on top)")] [Range(0, 30)]public float _ExtraEndlag = 0f;
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
}

[System.Serializable]
public class animationRigs
{
    public enum animationKey { Idle, Running, Attack, Knockback }
    public animationKey Key;
    public GameObject Rig;
    public Vector2 Offset;
}