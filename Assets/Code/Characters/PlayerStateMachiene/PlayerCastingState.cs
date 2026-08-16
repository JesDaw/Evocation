using UnityEngine;

/// <summary>
/// basically a state where the player cant move or attack but can be "controlled" and can freely switch characters and to freecam
/// maybe pressing space releses the state back the idle
/// the only state the player should be able to transition to at all times is the knockback state
/// </summary>
public class PlayerCastingState : PlayerBaseState
{
    public PlayerCastingState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;

    }
    public override void UpdateState()
    {
        CheckSwitchStates();
    }

    public override void EnterState()
    {
        HandleCasting();
    }

    public override void ExitState()
    {
        //if we want something to happen as the state is left
    }

    public override void InitializeSubState()
    {
        // if this gets substates
    }

    public void HandleCasting()
    {
        // startup animation
        // hold the frame where he is raising his hand, maybe play some particles or play a sound or something
        // the ending animation should be just the startup one but played in reverse and should be canslable into movement or attacing
    }
}
