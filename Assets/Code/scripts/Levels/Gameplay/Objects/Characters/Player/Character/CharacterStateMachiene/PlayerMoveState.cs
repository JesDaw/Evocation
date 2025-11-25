using UnityEngine;

public class PlayerMoveState : PlayerBaseState
{
    bool isFacingRight = true;

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
        //_context._Animator.SetBool("IsRunning", true);
        //_context._Animator.SetFloat("RunningSpeed", _context._ScrStats._AnimationMoveSpeed);
    }

    public override void ExitState()
    {
        //Debug.Log($"{Ctx.gameObject.name}: Exited MOVE state");
        Ctx.Rb.linearVelocityX = 0;
        Ctx.WalkingAudio.Stop();
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
            
            if (!Ctx.WalkingAudio.isPlaying) Ctx.WalkingAudio.Play();

            Ctx.Rb.linearVelocityX = horizontal * Ctx.PlayerStats._MoveSpeed;
            
            if (!isFacingRight && horizontal > 0f) Flip();
            else if (isFacingRight && horizontal < 0f) Flip();
        }
        else
        {
            Ctx.Rb.linearVelocity = new Vector2(0, Ctx.Rb.linearVelocity.y);
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = Ctx.transform.localScale;
        localScale.x *= -1f;
        Ctx.transform.localScale = localScale;
    }
}