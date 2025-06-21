using UnityEngine;

public class PlayerKnockedBackState : PlayerBaseState
{
    public PlayerKnockedBackState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory) { }

    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override void CheckSwitchStates()
    {
        // if player touches ground maybe
    }

    public override void EnterState()
    {
        HandleKnockback();
        SwitchState(Factory.Idle());
    }

    public override void ExitState()
    {
        //if we want something to happen as the state is left
    }

    public override void InitializeSubState()
    {
        // if this gets substates
    }

    void HandleKnockback()
    {
        
    }
}
