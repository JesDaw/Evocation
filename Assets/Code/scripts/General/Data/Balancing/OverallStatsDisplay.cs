using UnityEngine;

public class OverallStatsDisplay : MonoBehaviour
{
    public ClanStats ClanToDisplay;

    [Space]
    [Header("Clan Stats")]    
    public float AvgCosts;
    [Tooltip("MoveSpeed + KnockbackDamage")]
    public float PunchingPower;
    [Tooltip("AttackDamage / AttackEndlag")]
    public float DPS;
    [Tooltip("MaxHealth + KnockBackMaxHealth")]
    public float Defense;

    void Start() {}

    void Update() {}
}
