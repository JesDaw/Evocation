using UnityEngine;

public class PlayerAutoMoveState : PlayerBaseState
{
    const float ArrivalThreshold = 0.1f;

    public PlayerAutoMoveState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        if (Ctx.PlayerCommander.IsCmdPending(DiscretePlayerCommand.AutoMove))
            Ctx.PlayerCommander.TakePendingCmd(DiscretePlayerCommand.AutoMove);

        // Don't let a held direction fight the auto-move
        Ctx.PlayerCommander.SetActiveCmd(ContinuousPlayerCommand.Move, false, null);

        Ctx.Animator.SetBool("IsRunning", true);
        
        CameraControlSwitcher.Instance.SwitchToCameraControl(true);
    }

    public override void UpdateState()
    {
        MoveTowardTarget();
        CheckSwitchStates();
    }

    public override void CheckSwitchStates()
    {
        if (Ctx.PlayerCommander.IsCmdPending(DiscretePlayerCommand.Attack))
        {
            SwitchState(Factory.Attack());
        }
        else if (Ctx.PlayerCommander.IsCmdActive(ContinuousPlayerCommand.Move))
        {
            // A fresh directional press cancels auto-move
            SwitchState(Factory.Move());
        }
    }

    public override void ExitState()
    {
        Ctx.Animator.SetBool("IsRunning", false);
        Ctx.Rb.linearVelocityX = 0;
    }

    public override void InitializeSubState() { }

    void MoveTowardTarget()
    {
        if (AutoMoveLocation.Instance.Location == null)
        {
            SwitchState(Factory.Idle());
            return;
        }

        float delta = AutoMoveLocation.Instance.Location.x - Ctx.transform.position.x;

        if (Mathf.Abs(delta) <= ArrivalThreshold)
        {
            Ctx.Rb.linearVelocityX = 0;
            SwitchState(Factory.GetNextState(Ctx.PlayerCommander));
            return;
        }

        float dir = Mathf.Sign(delta);
        Ctx.Rb.linearVelocityX = dir * Ctx.PlayerStats._MoveSpeed;

        if (!Ctx.isFacingRight && dir > 0f) Flip();
        else if (Ctx.isFacingRight && dir < 0f) Flip();
    }
}