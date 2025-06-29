using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;

    }
    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override void CheckSwitchStates()
    {
        if (Ctx.IsClimbing) { SwitchState(Factory.Climb()); }
        else if (Ctx.IsKnockedBack) { SwitchState(Factory.KnockedBack()); }
        else if (Ctx.IsAttackPressed) { SwitchState(Factory.Attack()); }
        else if (Ctx.IsMovementPressed) { SwitchState(Factory.Move()); }
    }

    public override void EnterState()
    {
        HandleIdle();
    }

    public override void ExitState()
    {
        //if we want something to happen as the state is left
    }

    public override void InitializeSubState()
    {
        // if this gets substates
    }

    public void HandleIdle()
    {
        // idle animation or something
    }
}
