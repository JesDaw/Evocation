using UnityEngine;

public class CpuAttackState : CpuBaseState
{
    float _timer = 0f;
    enum AttackPhase { Startup, Cooldown, Done }
    AttackPhase _phase = AttackPhase.Startup;

    public CpuAttackState(CpuStateManager context) : base(context)
    {
        _context = context;
    }

    public override void EnterState()
    {
        _context._Animator.SetBool("IsAttacking", true);
        _phase = AttackPhase.Startup;
        _timer = 0;
    }

    public override void UpdateState()
    {
        Tick(Time.deltaTime);
    }

    public override void ExitState()
    {
        _context.UpdateCurrentState(CpuStateManager.State.Move);
    }

    public void Tick(float deltaTime)
    {
        _timer += deltaTime * 1000;

        switch (_phase)
        {
            case AttackPhase.Startup:
                if (_context._AnimatorController.ShouldAttack())
                {
                    AttackLogic.ExecuteAttack(_context);
                    //have attack sould be called from Ctx.AnimatorController with a signal
                    // but this is the function to call the attacking audio: FModAudioManager.instance.PlaySoundByName("attack");
                    _timer = 0f;
                    _phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                _context._Animator.SetBool("IsAttacking", false);
                if (_timer >= _context._Stats._AttackEndlag * 1000) 
                {
                    _phase = AttackPhase.Done;
                }
                break;

            case AttackPhase.Done:
                ExitState();
                break;
        }
    }
}