using UnityEngine;
using System.Collections.Generic;



public class CpuStateManager : MonoBehaviour
{
    public enum State
    {
        Move,
        Attack,
        KnockBack,
    }
    public Stats _Stats;
    public Stats _AttackingStats;
    public Rigidbody2D _Body;
    public Transform _Raycast;
    CpuBaseState _currentState;

    public Dictionary<State, CpuBaseState> _State = new Dictionary<State, CpuBaseState>();

    void Start()
    {
        _State[State.Move] = new CpuMoveState(this);
        _State[State.Attack] = new CpuAttackState(this);
        //_State[State.KnockBack] = new CpuKnockbackState(this);

        UpdateCurrentState(State.Move);
    }
    public void UpdateCurrentState(State state)
    {
        _currentState = _State[state];
        _currentState.EnterState();
    }

    void Update()
    {
        _currentState.UpdateState();
    }
}
