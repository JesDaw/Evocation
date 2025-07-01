using UnityEngine;

public class PlayerMoveState : PlayerBaseState
{
    float horizontal;
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
    
    public override void CheckSwitchStates()
    {
        if (!Ctx.IsAttackPressed && !Ctx.IsMovementPressed && !Ctx.IsClimbing && !Ctx.IsKnockedBack)
        { SwitchState(Factory.Idle()); }
        else if (Ctx.IsClimbing) { SwitchState(Factory.Climb()); }
        else if (Ctx.IsKnockedBack) { SwitchState(Factory.KnockedBack()); }
        else if (Ctx.IsAttackPressed) { SwitchState(Factory.Attack()); }
    }

    public override void EnterState()
    {
        
    }

    public override void ExitState()
    {
        //Ctx.Rb.linearVelocity = new Vector2(0, Ctx.Rb.linearVelocity.y);
        Ctx.Rb.linearVelocityX = 0;
        Ctx.WalkingAudio.Stop();
    }

    public override void InitializeSubState()
    {
        // if this gets substates
    }

    void HandleMove()
    {
        if (Ctx.ButtonContext.canceled) Ctx.Rb.linearVelocity = new Vector2(0, Ctx.Rb.linearVelocity.y);
        float input = Ctx.MovementContext;
        horizontal = input;

        
        if (!Ctx.WalkingAudio.isPlaying && !Ctx.ButtonContext.canceled) Ctx.WalkingAudio.Play();
        
        Ctx.Rb.linearVelocity = new Vector2(horizontal * Ctx.PlayerStats._MoveSpeed, Ctx.Rb.linearVelocity.y);
        if (!isFacingRight && horizontal > 0f) Flip();
        else if (isFacingRight && horizontal < 0f) Flip();
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = Ctx.transform.localScale;
        localScale.x *= -1f;
        Ctx.transform.localScale = localScale;
    }
}

