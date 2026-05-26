using UnityEngine;

public class PlayerAttackState : PlayerBaseState
{
    bool _attackOver = false;
    float _timer = 0f;
    enum AttackPhase { Startup, Cooldown, AnimationFinished, Done }
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
        switch (_phase)
        {
            case AttackPhase.Startup:
                if (Ctx.AnimatorController.ShouldAttack())
                {
                    if (_action != null && _target != null && !_target._IsDead)
                    {
                        CombatLogic.ExecuteAction(Ctx.PlayerStats, _action, _target);

                        if (Ctx.PlayerStats._ActionCooldownTimers != null &&
                            Ctx.PlayerStats._ActionCooldownTimers.Count > 0)
                        {
                            Ctx.PlayerStats._ActionCooldownTimers[0] =
                                Ctx.PlayerStats._ActionCooldown * _action.castCooldown;
                        }
                    }
                    _phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                AnimatorStateInfo stateInfo = Ctx._animator.GetCurrentAnimatorStateInfo(0);
                bool animDone = stateInfo.normalizedTime >= 1f && !Ctx._animator.IsInTransition(0);
                if (animDone) 
                {
                    _timer = 0f;
                    _phase = AttackPhase.AnimationFinished;
                }
                break;

            case AttackPhase.AnimationFinished:
                _timer += Time.deltaTime * 1000f;
                Ctx._animator.SetBool("IsAttacking", false);
                float endlagMs = Ctx._playerStats._ExtraEndlag * 1000f;
                if (_timer >= endlagMs) _phase = AttackPhase.Done;
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

    public override void ExitState()
    {
        Ctx.Animator.SetBool("IsAttacking", false);
        Ctx.PlayerCommander.ClearPendingCmds(DiscretePlayerCommand.Attack);
    }

    public override void InitializeSubState() { }
}