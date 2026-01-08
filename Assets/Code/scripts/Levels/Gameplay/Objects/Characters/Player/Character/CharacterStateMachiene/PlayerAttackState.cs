using UnityEngine;

public class PlayerAttackState : PlayerBaseState
{
    bool _attackOver = false;
    float _timer = 0f;
    enum AttackPhase { Startup, Cooldown, Done }
    AttackPhase _phase = AttackPhase.Startup;
    AttackType currentAttackType;

    public PlayerAttackState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        _attackOver = false;
        
        Ctx.PlayerCommander.TakePendingCmd(DiscretePlayerCommand.Attack);
        
        Ctx.Animator.SetBool("IsAttacking", true);
        
        currentAttackType = Ctx.ScrStats._AttackType;
        if(currentAttackType == null) 
        {
            Debug.LogWarning("<color=yellow>No AttackType on player</color>");
            _attackOver = true;
            return;
        }

        _phase = AttackPhase.Startup;
        _timer = 0f;
    }

    public override void UpdateState()
    {
        Tick(Time.deltaTime);
        CheckSwitchStates();
    }
    
    public override void CheckSwitchStates()
    {
        if (Ctx.IsKnockedBack) 
        { 
            SwitchState(Factory.KnockedBack()); 
            return;
        }
        
        if (_attackOver)
        {
            var nextState = Factory.GetNextState(Ctx.PlayerCommander);
            
            if (this.Equals(nextState))
            {
                EnterState();
            }
            else
            {
                SwitchState(nextState);
            }
        }
    }

    public void Tick(float deltaTime)
    {
        // Convert deltaTime to milliseconds
        _timer += deltaTime * 1000; 

        switch (_phase)
        {
            case AttackPhase.Startup:
                currentAttackType.boxSize = Ctx.PlayerStats._AttackRange;
                if (Ctx.AnimatorController.ShouldAttack())
                {
                    currentAttackType.Attack(Ctx); 
                    _timer = 0f;
                    _phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                Ctx.Animator.SetBool("IsAttacking", false);
                if (_timer >= Ctx.PlayerStats._AttackEndlag * 1000)
                {
                    _phase = AttackPhase.Done;
                }
                break;

            case AttackPhase.Done:
                if (Ctx.PlayerCommander.IsCmdPending(DiscretePlayerCommand.Attack))
                {
                    Ctx.PlayerCommander.ClearPendingCmds(DiscretePlayerCommand.Attack);
                    Ctx.PlayerCommander.SendCmd(DiscretePlayerCommand.Attack, null);
                }
                
                _attackOver = true;
                break;
        }
    }

    public override void ExitState()
    {
        Ctx.Animator.SetBool("IsAttacking", false);
    }

    public override void InitializeSubState()
    {
        // No substates needed
    }
}