using UnityEngine;

[System.Serializable]
public class NormalSpawn : CpuAction
{
    public AnimationCurve moneyCondition;
    public override bool EvalBasedOnCondition()
    {
        return false;
    }
    public override void UseAction()
    {

    }
}
