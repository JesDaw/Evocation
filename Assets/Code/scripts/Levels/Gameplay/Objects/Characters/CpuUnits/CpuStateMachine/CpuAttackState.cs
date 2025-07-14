
public class CpuAttackState : CpuBaseState
{
    public CpuAttackState(CpuStateManager context) : base(context)
    {
        _context = context;
    }

    public override void EnterState()
    {
        DealDamage();
    }

    public override void UpdateState()
    {

    }

    public override void ExitState()
    {

    }

    void DealDamage()
    {
        _context._AttackingStats.TakeDamage(_context._AttackingStats._AttackDamage);
        _context.UpdateCurrentState(CpuStateManager.State.Move);
    }
}
