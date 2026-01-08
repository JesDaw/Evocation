using UnityEngine;
using System.Collections.Generic;

public class CpuMoveState : CpuBaseState
{
    Rigidbody2D _Body;
    Stats _Stats;
    Transform _Transform;

    public CpuMoveState(CpuStateManager context) : base(context)
    {
        _context = context;
        _Body = _context._Body;
        _Stats = _context._Stats;
        _Transform = _context.transform;
    }

    public override void EnterState()
    {
        _context._Animator.SetBool("IsRunning", true);
        _context._Animator.SetFloat("RunningSpeed", _context._ScrStats._AnimationMoveSpeed);
    }

    public override void UpdateState()
    {
        Moving();
    }

    public override void ExitState()
    {
        _context.UpdateCurrentState(CpuStateManager.State.Attack);
    }

    void Moving()
    {
        _Body.linearVelocity = new Vector2(_Stats._MoveSpeed * _Transform.right.x, _Body.linearVelocity.y);

        List<Stats> targets = AttackDetection.FindTargetsInCircle(
            _Transform.position,
            _context._ScrStats._AttackType._StopDistance,
            _Stats.targetTags,
            _Stats
        );

        AttackDetection.DrawDebugCircle(_Transform.position, _context._ScrStats._AttackType._StopDistance, Color.yellow);

        if (targets.Count > 0)
        {
            _context._AttackingStats = targets[0];
            _Body.linearVelocity = Vector2.zero;
            ExitState();
        }
    }
}