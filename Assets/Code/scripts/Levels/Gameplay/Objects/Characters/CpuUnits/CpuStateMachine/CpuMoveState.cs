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

    float stopOffset = Random.Range(-0.3f, 0.3f);
    void Moving()
    {
        _Body.linearVelocity = new Vector2(_Stats._MoveSpeed * _Transform.right.x, _Body.linearVelocity.y);
        if(_context == null) Debug.Log("test");
        Collider2D[] hits = Physics2D.OverlapCircleAll(_Transform.position, _context._ScrStats._AttackType._StopDistance);
        DrawCircle(_Transform.position, _context._ScrStats._AttackType._StopDistance, Color.red);

        if (hits.Length <= 0) return;

        for (int I = 0; I < _Stats._CpuPriority.Count; I++)
        {
            for (int II = 0; II < hits.Length; II++)
            {
                if (hits[II].GetComponent<Collider2D>().CompareTag(_Stats._CpuPriority[I].ToString()))
                {
                    //actual attackers
                    GameObject EnemyGameobject = hits[II].GetComponent<Collider2D>().gameObject;
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

    void DrawCircle(Vector3 center, float radius, Color color, int segments = 32)
    {
        float angle = 0f;
        float increment = 360f / segments;

        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0f), Mathf.Sin(0f), 0f) * radius;

        for (int i = 1; i <= segments; i++)
        {
            angle += increment;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * radius;

            Debug.DrawLine(prevPoint, newPoint, color);
            prevPoint = newPoint;
        }
    }
}
