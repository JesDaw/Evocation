using UnityEngine;

public class PlayerKnockedBackState : PlayerBaseState
{
    private bool _Knocked;

    public PlayerKnockedBackState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
        BackOnGround();
    }

    public override void CheckSwitchStates() { }
    public override void InitializeSubState() { }

    public override void EnterState()
    {
        Ctx.Animator.SetBool("IsAttacking", false);
        Ctx.Animator.SetBool("IsRunning", false);
        Ctx.Animator.Play("Knockback", 0, 0f);
        Ctx.Animator.SetBool("IsKnockback", true);
        FModAudioManager.instance.PlaySoundByName("knockback");
        ApplyKnockback();
    }

    void ApplyKnockback()
    {
        if (Ctx.Rb == null) return;
        DamageSource lastHit = Ctx.PlayerStats.LastHitBy;
        
        bool knockedToTheLeft = false;
        if (lastHit != null && lastHit.sourcePosition != Vector3.zero)
        {
            float delta = Ctx.transform.position.x - lastHit.sourcePosition.x;
            knockedToTheLeft = delta >= 0f ? false : true;
        }
        else
        {
            knockedToTheLeft = Ctx.isFacingRight ? true : false;
        }
        bool shouldFaceRight = knockedToTheLeft;
        if (Ctx.isFacingRight != shouldFaceRight) Flip();

        float knockbackDir;
        if (knockedToTheLeft)
        { 
            knockbackDir = -1f;
        }
        else
        { 
            knockbackDir = 1f;
        }
        float angleRad = Ctx.ScrStats._KnockBackAngle * Mathf.Deg2Rad;
        float force    = Ctx.ScrStats._KnockBackVelocity;
        float forceX   = Mathf.Cos(angleRad) * force * knockbackDir;
        float forceY   = Mathf.Sin(angleRad) * force;

        Ctx.Rb.linearVelocity = Vector2.zero;
        Ctx.Rb.AddForce(new Vector2(forceX, forceY), ForceMode2D.Impulse);
        _Knocked = true;
    }

    void BackOnGround()
    {
        if (!_Knocked) return;
        if (Ctx.Rb.linearVelocity.y > 0) return;

        Collider2D col = Ctx.Rb.GetComponent<Collider2D>();
        if (col == null) return;

        Vector2 rayOrigin   = new Vector2(col.bounds.center.x, col.bounds.min.y);
        float   rayDistance = col.bounds.extents.y + 0.1f;

        RaycastHit2D groundCheck = Physics2D.Raycast(
            rayOrigin, Vector2.down, rayDistance,
            LayerMask.GetMask("Ground/TopLane", "Ground/MidLane", "Ground/BotLane"));

        if (groundCheck.collider != null)
            ExitState();
    }

    public override void ExitState()
    {
        if (!_Knocked) return;

        _Knocked = false;
        Ctx.Animator.SetBool("IsKnockback", false);

        if (Ctx.PlayerStats._IsDead)
        {
            FModAudioManager.instance.PlaySoundByName("die");
            Object.Destroy(Ctx.gameObject);
            return;
        }

        var resumeState = Ctx.ConsumePendingResumeState();
        var next = Factory.GetNextState(Ctx.PlayerCommander);

        // Only fall back to resuming auto-move if nothing more urgent
        // (a fresh KnockBack/Attack/AutoMove/Move) came in while airborne
        if (next == Factory.Idle() && resumeState != null)
            next = resumeState;

        SwitchState(next);
    }
}