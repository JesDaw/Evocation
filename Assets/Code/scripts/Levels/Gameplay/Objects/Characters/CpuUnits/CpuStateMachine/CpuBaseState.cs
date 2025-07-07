using UnityEngine;

public abstract class CpuBaseState
{
    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}
