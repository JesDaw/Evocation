using System.Collections.Generic;

public class PlayerStateFactory
{
    enum State
    {
        Idle,
        Move,
        Attack,
        KnockBack,
        Climb,
        Control,
        Auto
    }

    PlayerStateMachine _context;
    Dictionary<State, PlayerBaseState> _state = new Dictionary<State, PlayerBaseState>();

    public PlayerStateFactory(PlayerStateMachine CurrentContext)
    {
        _context = CurrentContext;

        _state[State.Idle] = new PlayerIdleState(_context, this);
        _state[State.Move] = new PlayerMoveState(_context, this);
        _state[State.Attack] = new PlayerAttackState(_context, this);
    }

    public PlayerBaseState Idle() { return _state[State.Idle]; }
    public PlayerBaseState Move() { return _state[State.Move]; }
    public PlayerBaseState Attack() { return _state[State.Attack]; }

    public PlayerBaseState KnockedBack()
    {
        if (!_state.ContainsKey(State.KnockBack))
        {
            _state[State.KnockBack] = new PlayerKnockedBackState(_context, this);
        }
        return _state[State.KnockBack];
    }

    public PlayerBaseState Climb()
    {
        if (!_state.ContainsKey(State.Climb))
        {
            _state[State.Climb] = new PlayerClimbState(_context, this);
        }
        return _state[State.Climb];

    }
    public PlayerBaseState Control()
    {
        if (!_state.ContainsKey(State.Control))
        {
            _state[State.Control] = new PlayerControlState(_context, this);
        }
        return _state[State.Control];
    }
    public PlayerBaseState Auto()
    {
        if (!_state.ContainsKey(State.Auto))
        {
            _state[State.Auto] = new PlayerControlState(_context, this);
        }
        return _state[State.Auto];
    }

    public PlayerBaseState GetNextState(PlayerCommander commander)
    {
        if (commander.IsCmdPending(DiscretePlayerCommand.KnockBack))
        {
            return KnockedBack();
        }
        else if (commander.IsCmdPending(DiscretePlayerCommand.Attack))
        {
            return Attack();
        }
        else if (commander.IsCmdActive(ContinuousPlayerCommand.Climb))
        {
            return Climb();
        }
        else if (commander.IsCmdActive(ContinuousPlayerCommand.Move))
        {
            return Move();
        }

        return Idle();
    }
}
