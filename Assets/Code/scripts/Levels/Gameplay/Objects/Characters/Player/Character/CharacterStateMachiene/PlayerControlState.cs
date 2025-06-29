using UnityEngine;

public class PlayerControlState : PlayerBaseState
{
    public PlayerControlState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
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
 
    }


}
