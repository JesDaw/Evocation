using System.Collections.Generic;

public class PlayerStateFactory
{
    enum State
    {
        Idle,
        Move,
        AutoMove,
        Attack,
        KnockBack,
        Animating,
    }

    PlayerStateMachine _context;
    Dictionary<State, PlayerBaseState> _state = new Dictionary<State, PlayerBaseState>();

    public PlayerStateFactory(PlayerStateMachine CurrentContext)
    {
        _context = CurrentContext;

        _state[State.Idle] = new PlayerIdleState(_context, this);
        _state[State.Move] = new PlayerMoveState(_context, this);
        _state[State.AutoMove] = new PlayerAutoMoveState(_context, this);
        _state[State.Attack] = new PlayerAttackState(_context, this);
        _state[State.Animating] = new PlayerAnimatingState(_context, this);
        
    }

    public PlayerBaseState Idle() { return _state[State.Idle]; }
    public PlayerBaseState Move() { return _state[State.Move]; }
    public PlayerBaseState AutoMove() { return _state[State.AutoMove]; }
    public PlayerBaseState Attack() { return _state[State.Attack]; }
    public PlayerBaseState Animating() { return _state[State.Animating]; }
    


    public PlayerBaseState KnockedBack()
    {
        if (!_state.ContainsKey(State.KnockBack))
        {
            _state[State.KnockBack] = new PlayerKnockedBackState(_context, this);
        }
        return _state[State.KnockBack];
    }

    public PlayerBaseState GetNextState(PlayerCommander commander)
    {
    if (commander.IsCmdPending(DiscretePlayerCommand.KnockBack)) return KnockedBack();
    else if (commander.IsCmdPending(DiscretePlayerCommand.AutoMove)) return AutoMove();
    else if (commander.IsCmdPending(DiscretePlayerCommand.Attack)) return Attack();
    else if (commander.IsCmdActive(ContinuousPlayerCommand.Move)) return Move();

    return Idle();
}
}
