using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
public class CpuStateManager : MonoBehaviour
{
    public enum State
    {
        Move,
        Attack,
        KnockBack,
    }
    public ScriptableStats _ScrStats;
    public Stats _Stats;
    public SpriteRenderer _Renderer;
    public Rigidbody2D _Body;
    public Transform _Raycast;
    CpuBaseState _currentState;

    public Dictionary<State, CpuBaseState> _State = new Dictionary<State, CpuBaseState>();
    [SerializeField] internal UltEvents.UltEvent<Stats> OnInitStats;

    [HideInInspector]
    public Stats _AttackingStats;

    void Start()
    {
        _Stats._Clan = _ScrStats._Clan;
        gameObject.tag = _Stats._Clan;
        _Stats._MaxHealth = _ScrStats._MaxHealth;
        _Stats._CurrentHealth = _ScrStats._CurrentHealth;
        _Stats._AttackDamage = _ScrStats._AttackDamage;
        _Stats._AttackEndlag = _ScrStats._AttackEndlag;
        _Stats._MoveSpeed = _ScrStats._MoveSpeed;

        //just looks better if they slightyoffset
        float randomNumber = Random.Range(-0.3f, 0.3f);
        _Stats._StopDistance = _ScrStats._StopDistance + randomNumber;
        _Stats._CpuPriority = _ScrStats._CpuPriority;

        //knockback
        _Stats._KnockBackHealth = _ScrStats._KnockBackHealth;
        _Stats._KnockBackVelocity = _ScrStats._KnockBackVelocity;
        _Stats._KnockBackMax = _ScrStats._KnockBackHealth;

        //status effects
        _Stats._StatusHealth = _ScrStats._StatusHealth;
        _Stats._StatusMax = _ScrStats._StatusHealth;
        
        _Renderer.sprite = _ScrStats._Sprite;

        OnInitStats.Invoke(_Stats);


        _State[State.Move] = new CpuMoveState(this);
        _State[State.Attack] = new CpuAttackState(this);

        _State[State.KnockBack] = new CpuKnockBackState(this);

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
