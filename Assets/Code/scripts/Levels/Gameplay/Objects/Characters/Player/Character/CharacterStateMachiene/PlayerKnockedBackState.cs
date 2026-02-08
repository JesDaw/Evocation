using UnityEngine;

public class PlayerKnockedBackState : PlayerBaseState
{
    private bool _Knocked;
    public PlayerKnockedBackState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;

    }

    public override void UpdateState()
    {
        CheckSwitchStates();
        BackOnGround();
    }
    public override void CheckSwitchStates()
    {
        // if player touches ground maybe
    }
    public override void InitializeSubState()
    {

    }

    public override void EnterState()
    {
        Ctx.Animator.SetBool("IsKnockback", true);
        FModAudioManager.instance.PlaySoundByName("knockback");
        ApplyKnockback();
    }
    void ApplyKnockback()
    {
        if (Ctx.Rb != null)
        {
            short knockbackDir = -1;
            if (!Ctx.isFacingRight) knockbackDir = 1; 
            Vector2 knockbackForce = new Vector2(knockbackDir * Ctx.ScrStats._KnockBackVelocity,
                                                                Ctx.ScrStats._KnockBackVelocity);
            Ctx.Rb.linearVelocity = Vector2.zero;
            Ctx.Rb.AddForce(knockbackForce, ForceMode2D.Impulse);
            _Knocked = true;
        }
    }

    void BackOnGround()
    {
        if (Mathf.Abs(Ctx.Rb.linearVelocity.y) < 0.1f)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        if(_Knocked)
        {
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
}
