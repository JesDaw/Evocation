using UnityEngine;

public class PlayerMoveState : PlayerBaseState
{
    public PlayerMoveState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }
    
    public override void UpdateState()
    {
        HandleMove();
        CheckSwitchStates();
    }

    public override void EnterState()
    {
        Ctx.Animator.SetBool("IsRunning", true);
        //Ctx.Animator.SetFloat("RunningSpeed", Ctx.ScrStats._AnimationMoveSpeed);
        if (Ctx.DebugLogs) Debug.Log($"Player is running in move state");
    }

    public override void ExitState()
    {
        //Debug.Log($"{Ctx.gameObject.name}: Exited MOVE state");
        Ctx.Animator.SetBool("IsRunning", false);
        Ctx.Rb.linearVelocityX = 0;
    }

    public override void InitializeSubState()
    {
        // if this gets substates
    }

    void HandleMove()
    {
        if (Ctx.IsMovementPressed)
        {
            float horizontal = Ctx.MovementContext;
            //Debug.Log($"{Ctx.gameObject.name}: Moving - Horizontal: {horizontal}, IsMovementPressed: {Ctx.IsMovementPressed}");
            

            Ctx.Rb.linearVelocityX = horizontal * Ctx.PlayerStats._MoveSpeed;
            
            if (!Ctx.isFacingRight && horizontal > 0f) Flip();
            else if (Ctx.isFacingRight && horizontal < 0f) Flip();
        }
        else
        {
            Ctx.Rb.linearVelocity = new Vector2(0, Ctx.Rb.linearVelocity.y);
        }
    }

}