using UnityEngine;

[CreateAssetMenu(fileName = "DefaultAttack", menuName = "AttackType/DefaultAttack")]
public class DefaultAttackType : AttackType
{
    public override void Attack(CpuStateManager _context)
    {
        DealDamage(_context);
    }
}
