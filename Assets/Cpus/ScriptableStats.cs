using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Cpu", menuName = "Cpu", order = 0)]
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
    public int _spawnCost;
    public List<StatusEffect> _EffectsToApply;
    public int _StatusHealth = 1;
    // anyways all of the events script that happen on the cpu
    // uses the cpu utilits script
    //so just update that if you're wondering aobu the different projectiles

    //also this is the Scriptable obejct change the actual stats script lmao.
    public List<int> OnAttack;
    //dude I wish i was a better programmer
    //but ahahhahahah I don't know how else to do it
    //but the current logic is that it's spilt between the Cpu Utilis
    //so this is just for like extra crap just incase
    public ScriptableStats ExtraStats;
}
    
