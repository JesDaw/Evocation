using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerMoveState : PlayerBaseState
{
    /*
    public Rigidbody2D rb;
    public Transform groundCheck;
    public LayerMask groundLayer;
*/
    private float horizontal;
    //  public float distance;
    private bool isFacingRight = true;


    [SerializeField] AudioSource walking_audio;
    public PlayerMoveState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }
    public override void UpdateState()
    {
        CheckSwitchStates();
        HandleMove();
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
        HandleMove();
    }

    public override void ExitState()
    {
        Ctx.Rb.linearVelocity = new Vector2(0, 0);

        //    walking_audio.Stop();
    }

    public override void InitializeSubState()
    {
        // if this gets substates
    }

    void HandleMove()
    {
        float input = Ctx.MovementContext;
        horizontal = input;
        //if (!walking_audio.isPlaying)
        //{
        //walking_audio.Play();
        //}
      
        Ctx.Rb.linearVelocity = new Vector2(horizontal * Ctx.PlayerStats._MoveSpeed, Ctx.Rb.linearVelocity.y);
        //Ctx.Rb.linearVelocity = new Vector2(horizontal * Ctx.PlayerStats._MoveSpeed, Ctx.Rb.linearVelocity.y);
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

