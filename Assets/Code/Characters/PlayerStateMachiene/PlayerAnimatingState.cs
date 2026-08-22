using UnityEngine;

/// <summary>
/// General-purpose state for playing a specific animation by id - covers emotes, item
/// pickup, and "hold a pose" states like a spell-cast windup. Which animation plays is
/// chosen by whoever switches the player into this state (see PlayerStateMachine.PlayAnimationState).
///
/// Optional startup/ending clips play on enter/exit. The main clip either plays once and
/// auto-exits (discrete) or loops until RequestEnd() is called (continuous). Optionally
/// cancelable early via the Attack button. Regardless of how it ends, this state always
/// exits to Idle - never Attack - even when canceled with the attack button.
///
/// KnockBack can still interrupt this state at any time via
/// PlayerStateMachine.UpdateCurrentStateToKnockback, same as every other state.
/// </summary>
public class PlayerAnimatingState : PlayerBaseState
{
    enum Phase { Startup, Main, Ending }

    PlayerAnimationDefinition _definition;
    Phase _phase;
    bool _isActive;
    bool _endRequested;

    public PlayerAnimatingState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    /// <summary>Configures which animation to play. Call before EnterState() runs.</summary>
    public void QueueAnimation(PlayerAnimationDefinition definition)
    {
        _definition = definition;
    }

    /// <summary>For continuous animations - ends the loop and moves to Ending/exit.</summary>
    public void RequestEnd()
    {
        _endRequested = true;
    }

    public override void EnterState()
    {
        if (_definition == null)
        {
            Debug.LogWarning("[PlayerAnimatingState] Entered with no animation queued, exiting to Idle.");
            SwitchState(Factory.Idle());
            return;
        }
        Ctx.Animator.SetBool("IsRunning", false);
        Ctx.Animator.SetBool("IsAttacking", false);

        _isActive = true;
        _endRequested = false;
        Ctx.Rb.linearVelocity = Vector2.zero;

        if (!string.IsNullOrEmpty(_definition.StartupAnimation))
        {
            _phase = Phase.Startup;
            Ctx.Animator.Play(_definition.StartupAnimation, 0, 0f);
        }
        else
        {
            BeginMain();
        }
    }

    public override void UpdateState()
    {
        if (_phase != Phase.Ending && _definition.Cancelable && Ctx.PlayerCommander.IsCmdPending(DiscretePlayerCommand.Attack)) //Change if we want to to some other button idk
        {
            // Consume the press here so it doesn't leak into a real attack once we're back in Idle
            Ctx.PlayerCommander.TakePendingCmd(DiscretePlayerCommand.Attack);
            BeginEnding();
            return;
        }

        switch (_phase)
        {
            case Phase.Startup:
                if (ClipFinished()) BeginMain();
                break;

            case Phase.Main:
                if (_definition.IsContinuous)
                {
                    if (_endRequested) BeginEnding();
                }
                else if (ClipFinished())
                {
                    BeginEnding();
                }
                break;

            case Phase.Ending:
                if (ClipFinished()) ExitState();
                break;
        }
    }

    public override void CheckSwitchStates() { }
    public override void InitializeSubState() { }

    public override void ExitState()
    {
        if (!_isActive) return;
        _isActive = false;

        Ctx.Rb.linearVelocityX = 0;
        SwitchState(Factory.Idle());
    }

    void BeginMain()
    {
        _phase = Phase.Main;
        if (!string.IsNullOrEmpty(_definition.MainAnimation)) Ctx.Animator.Play(_definition.MainAnimation, 0, 0f);
    }

    void BeginEnding()
    {
        if (!string.IsNullOrEmpty(_definition.EndingAnimation))
        {
            _phase = Phase.Ending;
            Ctx.Animator.Play(_definition.EndingAnimation, 0, 0f);
        }
        else
        {
            ExitState();
        }
    }

    bool ClipFinished()
    {
        var info = Ctx.Animator.GetCurrentAnimatorStateInfo(0);
        return !Ctx.Animator.IsInTransition(0) && info.normalizedTime >= 1f;
    }
}

[System.Serializable]
public class PlayerAnimationDefinition
{
    [Tooltip("Optional. Played once on state enter, before Main.")]
    public string StartupAnimation;

    [Tooltip("The main clip. Looped if IsContinuous, played once otherwise.")]
    public string MainAnimation;

    [Tooltip("Optional. Played once on exit, after Main.")]
    public string EndingAnimation;

    [Tooltip("True = Main loops until RequestEnd() is called externally. False = auto-exits once Main finishes.")]
    public bool IsContinuous;

    [Tooltip("True = pressing Attack cuts the animation short (skips to Ending / exits).")]
    public bool Cancelable;
}