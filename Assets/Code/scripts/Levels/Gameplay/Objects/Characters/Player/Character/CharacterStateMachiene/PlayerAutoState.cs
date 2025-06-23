using UnityEngine;

public class PlayerAutoState : PlayerBaseState
{
    public PlayerAutoState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
        InitializeSubState();
    }
    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override void CheckSwitchStates()
    {
        throw new System.NotImplementedException();
    }

    public override void EnterState()
    {
        throw new System.NotImplementedException();
    }

    public override void ExitState()
    {
        throw new System.NotImplementedException();
    }

    public override void InitializeSubState()
    {
        throw new System.NotImplementedException();
    }


}
