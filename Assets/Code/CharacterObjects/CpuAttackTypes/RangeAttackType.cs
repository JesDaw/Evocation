using System;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "RangeAttack", menuName = "AttackType/RangeAttack")]
public class RangeAttackType : AttackType
{
    public enum ProjType {Arch, Direct}
    public ProjType projType;
    public AnimationCurve projectileCurve;
    public AnimationCurve projectileSpeedCurve;
    public Vector2 detectionRange;
    public override void Attack(CpuStateManager _context)
    {
        if(projType == ProjType.Arch)
            throw new System.NotImplementedException();

    }
}
