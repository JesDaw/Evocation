using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "RangeAttack", menuName = "AttackType/RangeAttack")]
public class RangeAttackType : AttackType
{
    public Sprite attackApperance;
    public AnimationCurve projectileCurve;
    public float speed;
    public float offset = 2;
    public GameObject projObject;

    public override void Attack(CpuStateManager _context)
    {
        GameObject createdProj = Instantiate(projObject);

        Projectile projectile = createdProj.GetComponent<Projectile>();
        projectile.Launch(
            _context.transform.position,
            _context._AttackingStats.transform,
            projectileCurve,
            speed,
            offset,
            () => DealDamage(_context)
        );
    }
}
