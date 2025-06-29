using UnityEngine;

public class PlayerClimbState : PlayerBaseState
{
    public PlayerClimbState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;

    }
    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override void CheckSwitchStates()
    {
        if (!Ctx.IsAttackPressed && !Ctx.IsMovementPressed && !Ctx.IsClimbing && !Ctx.IsKnockedBack)
        { SwitchState(Factory.Idle()); }
    }


    public override void EnterState()
    {
        throw new System.NotImplementedException();
    }

    public override void ExitState()
    {
        //if we want something to happen as the state is left
        // reset gravity scale to 4
    }

    public override void InitializeSubState()
    {
        // if this gets substates
    }


}
