using System.Collections;
using UnityEngine;

public class CpuKnockBackState : CpuBaseState
{
    Rigidbody2D rb;
    Stats _Stats;
    bool _Knocked;
    public CpuKnockBackState(CpuStateManager context) : base(context)
    {
        _context = context;
        _Stats = _context._Stats;
        rb = _Stats.gameObject.GetComponent<Rigidbody2D>();
    }

    public override void EnterState()
    {
        Debug.Log("enter Knockback");
        _context._Animator.SetBool("IsKnockback", true);
        ApplyKnockback();
    }

    public override void UpdateState()
    {
        BackOnGround();
    }

    public override void ExitState()
    {
        _context.UpdateCurrentState(CpuStateManager.State.Move);
        _context._Animator.SetBool("IsKnockback", false);
    }

    void ApplyKnockback()
    {
        if (rb != null)
        {
            short knockbackDir = -1;
            if (_context._Stats._Enemy) knockbackDir = 1; 
            Vector2 knockbackForce = new Vector2(knockbackDir * _context._ScrStats._KnockBackVelocity,
                                                                _context._ScrStats._KnockBackVelocity);
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackForce, ForceMode2D.Impulse);
            _Knocked = true;
        }
    }

    void BackOnGround()
    {
        if (!_Knocked) return;
        if ((Mathf.Abs(rb.linearVelocity.y) < 0.1f))
        {
            ExitState();
        }
    }
}

