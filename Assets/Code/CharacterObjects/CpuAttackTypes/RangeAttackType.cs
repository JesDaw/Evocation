using UnityEngine;

[CreateAssetMenu(fileName = "RangeAttack", menuName = "AttackType/RangeAttack")]
public abstract class RangeAttackType : AttackType
{
    public AnimationCurve projectileCurve;
}
