using UnityEngine;
using System.Collections.Generic;

public class CpuStateManager : MonoBehaviour
{
    CpuBaseState _currentState;

    public enum State
    {
        Idle,
        Move,
        Attack,
        KnockBack
    }

    public Dictionary<State, CpuBaseState> _State;

    void Start()
    {
        _State[State.Idle] = new CpuIdleState(this);
        _State[State.Move] = new CpuMoveState(this);
        _State[State.Attack] = new CpuAttackState(this);
        //_State[State.KnockBack] = new CpuKnockbackState(this);

        _currentState = _State[State.Idle];
        _currentState.EnterState();    
    }
}
