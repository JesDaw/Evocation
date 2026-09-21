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

    public override void EnterState()
    {
        /*
        if(Ctx.Animator != null)
        {
            Ctx.Animator.Rebind();
            Ctx.Animator.Update(0f);
        }
        */

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
