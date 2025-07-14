using UnityEngine;

public abstract class CpuBaseState
{
    public CpuStateManager _context;
    public CpuBaseState(CpuStateManager context)
    {
        _context = context;
    }
    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}
