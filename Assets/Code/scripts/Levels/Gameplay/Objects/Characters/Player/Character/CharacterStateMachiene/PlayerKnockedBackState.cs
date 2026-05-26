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

        // ── Direction: always away from whoever hit us ─────────────────────
        // sourcePosition is populated by CombatLogic on every damaging action.
        // If it's zero (e.g. a status-effect tick with no positional source)
        // we fall back to the facing-based heuristic that was here before.
        DamageSource lastHit = Ctx.PlayerStats.LastHitBy;
        float knockbackDir;

        if (lastHit != null && lastHit.sourcePosition != Vector3.zero)
        {
            // Positive delta → player is to the RIGHT of the attacker → knock right (+1)
            // Negative delta → player is to the LEFT  of the attacker → knock left  (-1)
            float delta = Ctx.transform.position.x - lastHit.sourcePosition.x;
            knockbackDir = delta >= 0f ? 1f : -1f;
        }
        else
        {
            // Fallback: knocked opposite to current facing (original behaviour)
            knockbackDir = Ctx.isFacingRight ? -1f : 1f;
        }

        // ── Flip to face away before the impulse ──────────────────────────
        // shouldFaceRight = true  → knocked right → player looks right (away from left-side attacker)
        // shouldFaceRight = false → knocked left  → player looks left  (away from right-side attacker)
        bool shouldFaceRight = knockbackDir > 0f;
        if (Ctx.isFacingRight != shouldFaceRight)
            Flip();

        // ── Apply impulse using the same angle-based math as CpuKnockBackState ─
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
        }
        else
        {
            SwitchState(Factory.Idle());
        }
    }
}