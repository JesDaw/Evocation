using System.Collections.Generic;

public class CpuStateFactory
{
    State _currentState;

    public enum State
    {
        Idle,
        Move,
        Attack,
        KnockBack
    }
    CpuStateMachine _context;
    Dictionary<State, CpuBaseState> _state = new Dictionary<State, CpuBaseState>();
    public CpuStateFactory(CpuStateMachine CurrentContext)
    {
        _context = CurrentContext;
    }

    public CpuBaseState Idle() { return _state[State.Idle]; }
    public CpuBaseState Move() { return _state[State.Move]; }
    public CpuBaseState Attack() { return _state[State.Attack]; }
}
