using UnityEngine;

public static class LevelBalancerMath
{
    public static float CalculateLevel(ScriptableStats s, BalancingGrapher g)
    {
        float totalAttackDuration = s._AnimationStartupTime + s._AnimationRecoveryTime + (s._ExtraEndlag * 1000f);

        float attack = s._AttackDamage + s._KnockBackDamage;
        float defense = s._MaxHealth + s._KnockBackMaxHealth;
        float spaceControl = (s._HorizontalRange / g.K_Range) + (s._MoveSpeed / g.K_MoveSpeed);
        float attackFrequency = (g.K_AttackRate * 1000f) / totalAttackDuration;

        return attack + defense + spaceControl + attackFrequency;
    }

    public static (float attack, float defense, float spaceControl, float attackFrequency, float total) GetLevelBreakdown(ScriptableStats s, BalancingGrapher g)
    {
        float totalAttackDuration = s._AnimationStartupTime + s._AnimationRecoveryTime + (s._ExtraEndlag * 1000f);

        float attack = s._AttackDamage + s._KnockBackDamage;
        float defense = s._MaxHealth + s._KnockBackMaxHealth;
        float spaceControl = (s._HorizontalRange / g.K_Range) + (s._MoveSpeed / g.K_MoveSpeed);
        float attackFrequency = (g.K_AttackRate * 1000f) / totalAttackDuration;

        float total = attack + defense + spaceControl + attackFrequency;

        return (attack, defense, spaceControl, attackFrequency, total);
    }
}
