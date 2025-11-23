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

        RaycastHit2D[] hits = Physics2D.RaycastAll(_Raycast.position, _Transform.right, _Stats._StopDistance);
        Debug.DrawRay(_Raycast.position, _Transform.right * _Stats._StopDistance, Color.red);

        if (hits.Length <= 0) return;

        for (int I = 0; I < _Stats._CpuPriority.Count; I++)
        {
            for (int II = 0; II < hits.Length; II++)
            {
                if (hits[II].collider.CompareTag(_Stats._CpuPriority[I].ToString()))
                {
                    //actual attackers
                    GameObject EnemyGameobject = hits[II].collider.gameObject;
                    _context._AttackingStats = EnemyGameobject.GetComponent<Stats>();
                    if (_context._AttackingStats == null) Debug.LogWarning("Make sure the enemy has their collider and stats script on the same object");
                    //this section is for healing

                    _Body.linearVelocity = Vector2.zero;
                    //heal

                    if (_context._AttackingStats == null)
                    {
                        Debug.Log("No stats object attached");
                        continue;
                    }

                    ExitState();             
                }
            }
        }
    }
}
