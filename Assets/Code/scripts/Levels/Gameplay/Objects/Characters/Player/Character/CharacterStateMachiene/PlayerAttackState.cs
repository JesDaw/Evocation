using UnityEngine;

public class PlayerAttackState : PlayerBaseState
{
    bool _attackOver = false;
    float _timer = 0f;
    enum AttackPhase { Startup, Cooldown, Done }
    AttackPhase _phase = AttackPhase.Startup;

    public PlayerAttackState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        _attackOver = false;
        Ctx.PlayerCommander.TakePendingCmd(DiscretePlayerCommand.Attack);
        Ctx.Animator.SetBool("IsAttacking", true);
        _phase = AttackPhase.Startup;
        _timer = 0f;
    }

    public override void UpdateState()
    {
        _timer += Time.deltaTime * 1000; 

        switch (_phase)
        {
            case AttackPhase.Startup:
                if (Ctx.AnimatorController.ShouldAttack())
                {
                    AttackLogic.ExecuteAttack(Ctx);
                    _timer = 0f;
                    _phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                Ctx.Animator.SetBool("IsAttacking", false);
                if (_timer >= Ctx.PlayerStats._AttackEndlag * 1000) _phase = AttackPhase.Done;
                break;

            case AttackPhase.Done:
                _attackOver = true;
                break;
        }
        CheckSwitchStates();
    }
    
    public override void CheckSwitchStates()
    {
        if (Ctx.IsKnockedBack) SwitchState(Factory.KnockedBack());
        else if (_attackOver) SwitchState(Factory.GetNextState(Ctx.PlayerCommander));
    }

    public override void ExitState() => Ctx.Animator.SetBool("IsAttacking", false);
    public override void InitializeSubState() { }
}