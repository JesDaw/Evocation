using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stats", menuName = "Npc", order = 0)]
public class ScriptableStats : ScriptableObject
{
    public List<string> _CpuPriority;
    public Sprite _Sprite;
    public string _Clan;
    public int _Health;
    public int _Attack;
    public float _AttackSpeed;
    public float _Speed;
    public float _StopDistance;
    public float _KnockBackVelocity;
    public float _KnockBackHealth;
}
    
