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

    public override void UpdateState() => Moving();
    public override void ExitState() => _context.UpdateCurrentState(CpuStateManager.State.Attack);

    void Moving()
    {
        _Body.linearVelocity = new Vector2(_Stats._MoveSpeed * _Transform.right.x, _Body.linearVelocity.y);

        Vector2 range = _Stats._AttackRange;
        bool facingLeft = _Transform.right.x < 0;

        Vector2 detectionCenter = AttackLogic.CalculateAttackCenter(_Transform.position, facingLeft, range);

        List<IDamageable> targets = AttackDetection.FindTargetsInBox(detectionCenter, range, _Stats.targetTags, _Stats);
        AttackDetection.DrawDebugBox(detectionCenter, range, Color.yellow);

        if (targets.Count > 0)
        {
            _context._AttackingStats = targets[0] as Stats;
            _Body.linearVelocity = Vector2.zero;
            ExitState();
        }

        bool isGrounded = Physics2D.Raycast(_Body.transform.position, Vector2.down, 0.1f);
        if(!isGrounded)
            _Body.gravityScale = 100.0f;
        else
            _Body.gravityScale = 1.0f;

    }
}
