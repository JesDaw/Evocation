using UnityEngine;

[CreateAssetMenu(fileName = "ClanStats", menuName = "Clan Stats")]
public class ClanStats : ScriptableObject
{
    public ScriptableStats[] all_stats_scripts;

    [Header("Avaeage cost")]
    public float AvgCost;
    [Space]

    [Header("Summary")]
    [Tooltip("MoveSpeed + KnockbackDamage")]
    public float PunchingPower;
    [Tooltip("AttackDamage / AttackEndlag")]
    public float DPS;
    [Tooltip("MaxHealth + KnockBackMaxHealth")]
    public float Defense;

    [Space]
    [Header("Individual Averages")]
    public float AvgMoveSpeed;
    public float AvgKnockbackDamage;
    public float AvgAttackDamage;
    public float AvgAttackEndlag;
    public float AvgMaxHP;
    public float AvgKnockbackMaxHP;

    void OnEnable() => UpdateAverages();

    public void UpdateAverages()
    {
        if (all_stats_scripts == null || all_stats_scripts.Length == 0) 
        {
            Debug.LogWarning($"[Average Clan Stats]: Aint nothin in that bih");
            return;
        }

        float totalCost = 0;
        float totalMoveSpeed = 0;
        float totalKnockbackDamage = 0;
        float totalAttackDamage = 0;
        float totalAttackEndlag = 0;
        float totalMaxHP = 0;
        float totalKnockbackMaxHP = 0;

        foreach (ScriptableStats s in all_stats_scripts)
        {
            totalCost             += s._spawnCost;
            totalMoveSpeed        += s._MoveSpeed;
            totalKnockbackDamage  += s._KnockBackDamage;
            totalAttackDamage     += s._AttackDamage;
            totalAttackEndlag     += s._AttackEndlag;
            totalMaxHP            += s._MaxHealth;
            totalKnockbackMaxHP   += s._KnockBackMaxHealth;
        }

        int count = all_stats_scripts.Length;

        AvgCost            = totalCost            / count;
        AvgMoveSpeed       = totalMoveSpeed       / count;
        AvgKnockbackDamage = totalKnockbackDamage / count;
        AvgAttackDamage    = totalAttackDamage    / count;
        AvgAttackEndlag    = totalAttackEndlag    / count;
        AvgMaxHP           = totalMaxHP           / count;
        AvgKnockbackMaxHP  = totalKnockbackMaxHP  / count;

        PunchingPower = AvgMoveSpeed + AvgKnockbackDamage;
        DPS           = AvgAttackEndlag > 0 ? AvgAttackDamage / AvgAttackEndlag : 0f;
        Defense       = AvgMaxHP + AvgKnockbackMaxHP;
    }
}