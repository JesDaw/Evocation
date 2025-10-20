using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stats", menuName = "Stats", order = 0)]
public class ScriptableStats : ScriptableObject
{
    public List<string> _CpuPriority;
    public Sprite _Sprite;
    public string _Clan;
    public int _MaxHealth = 1;
    public int _CurrentHealth = 1;
    public int _AttackDamage;
    public int _AttackStartup;
    public int _AttackActiveDuration;
    public int _AttackEndlag;
    public float _MoveSpeed;
    public float _StopDistance;
    public float _KnockBackMax = 1;
    public float _KnockBackHealth;
    public float _KnockBackVelocity;
    public int _spawnCost;
    public List<StatusEffect> _StatusEffects;
    //x = Tick
    //y = Length
    public List<StatusEffect> _EffectsToApply;
    public List<Vector2> _StatusTicksMax;
    public List<Vector2> _StatusTicks;
    public int _StatusMax;
    public int _StatusHealth;
    // anyways all of the events script that happen on the cpu
    // uses the cpu utilits script
    //so just update that if you're wondering aobu the different projectiles

    //also this is the Scriptable obejct change the actual stats script lmao.
    public List<int> OnAttack;
    //!-- CHECK CPU ULTILS FOR ON ATTACK FUNC --!//
    // 0 - spawn mobs from extra stats
    // 1 - shoot projectiles from extra proj

    //dude I wish i was a better programmer
    //but ahahhahahah I don't know how else to do it
    //but the current logic is that it's spilt between the Cpu Utilis
    //so this is just for like extra crap just incase
    public ScriptableStats ExtraStats;
}
    
