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
        ApplyKnockback();
    }

    public override void UpdateState()
    {
        BackOnGround();
    }

    public override void ExitState()
    {
        _context.UpdateCurrentState(CpuStateManager.State.Move);
    }

    void ApplyKnockback()
    {
        if (rb != null)
        {
            Vector2 knockbackForce = new Vector2(-_Stats._KnockBackVelocity, _Stats._KnockBackVelocity);
            rb.AddForce(knockbackForce, ForceMode2D.Impulse);
            _Knocked = true;
        }
    }

    void BackOnGround()
    {
        if (!_Knocked) return;
        if (Mathf.Abs(rb.linearVelocity.y) < 0.01f)
        {
            ExitState();
        }
    }
}

