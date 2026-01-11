using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class CpuStateManager : MonoBehaviour
{
    public enum State
    {
        Move,
        Attack,
        KnockBack,
    }

    [Header("References")]
    public Stats _Stats;
    public Rigidbody2D _Body;
    public Animator _Animator;
    public AnimationEventsController _AnimatorController;

    [Header("State Management")]
    CpuBaseState _currentState;
    public Dictionary<State, CpuBaseState> _State = new Dictionary<State, CpuBaseState>();

    [HideInInspector] public Stats _AttackingStats;
    public ScriptableStats _ScrStats => _Stats.scriptableStats;

    void Start()
    {
        toggleEverything(false);
        _Stats.InitializeStats();

        _State[State.Move] = new CpuMoveState(this);
        _State[State.Attack] = new CpuAttackState(this);
        _State[State.KnockBack] = new CpuKnockBackState(this);
    }

    void toggleEverything(bool enable)
    {
        var components = GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (comp != this)
            {
                comp.enabled = enable;
            }
        }
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(enable);
        }
    }

    public void UpdateCurrentState(State state)
    {
        if(_Animator != null)
            _Animator.Rebind();

        _currentState = _State[state];
        _currentState.EnterState();
    }

    void Update()
    {
        _currentState?.UpdateState();
    }
}