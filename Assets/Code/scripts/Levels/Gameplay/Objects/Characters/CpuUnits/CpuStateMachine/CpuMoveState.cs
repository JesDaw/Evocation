using UnityEngine;
using System.Collections.Generic;
public class CpuMoveState : CpuBaseState
{
    Rigidbody2D _Body;
    Stats _Stats;
    Transform _Transform;
    Transform _Raycast;
    public CpuMoveState(CpuStateManager context) : base(context)
    {
        _context = context;
        _Body = _context._Body;
        _Stats = _context._Stats;
        _Transform = _context.transform;
        _Raycast = _context._Raycast;
    }
    public override void EnterState()
    {
    }
    public override void UpdateState()
    {
        Moving();
    }
    public override void ExitState()
    {
    }

    void Moving()
    {
        _Body.linearVelocity = new Vector2(_Stats._MoveSpeed * _Transform.right.x, _Body.linearVelocity.y);

        RaycastHit2D[] hits = Physics2D.RaycastAll(_Raycast.position, _Transform.right, _Stats._StopDistance);
        Debug.DrawRay(_Raycast.position, _Transform.right * _Stats._StopDistance, Color.red);

        if (hits.Length <= 0) return;

        _Body.linearVelocity = Vector2.zero;
        _context.UpdateCurrentState(CpuStateManager.State.Attack);
    }
}
