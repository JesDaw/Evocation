using UnityEngine;

public class PlayerControlState : PlayerBaseState
{
    public PlayerControlState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        Debug.Log("Control state Constructor activated");
        IsRootState = true;
        InitializeSubState();
        Debug.Log("Control state Constructor complete");
    }
    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override void CheckSwitchStates()
    {
        // if the player isnt controling the charater switch states
    }

    public override void EnterState()
    {

    }

    public override void ExitState()
    {
        //if we want something to happen as the state is left
    }

    public override void InitializeSubState()
    {
        if (!Ctx.IsAttackPressed && !Ctx.IsMovementPressed && !Ctx.IsClimbing && !Ctx.IsKnockedBack)
        { SetSubState(Factory.Idle()); }
        else if (Ctx.IsClimbing) { SetSubState(Factory.Climb()); }
        else if (Ctx.IsKnockedBack) { SetSubState(Factory.KnockedBack()); }
        else if (Ctx.IsAttackPressed) { SetSubState(Factory.Attack()); }
        else if (Ctx.IsMovementPressed) { SetSubState(Factory.Move()); }
    }


}
