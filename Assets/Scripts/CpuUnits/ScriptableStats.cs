using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stats", menuName = "Npc", order = 0)]
public class ScriptableStats : ScriptableObject
{
    public List<string> _CpuPriority;
    public Sprite _Sprite;
    public string _Clan;
    public int _Health;
    //for healing just change attack to a negaitve number
    public int _Attack;
    public float _AttackSpeed;
    public float _Speed;
    public float _StopDistance;
    public float _KnockBackVelocity;
    public float _KnockBackHealth;
    public enum AttackType {Projectile, AOE, Direct}
    public AttackType _AttackType = AttackType.Direct;
}
    
