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
        _context._Animator.SetBool("IsAttacking", false);
        _context._Animator.SetBool("IsRunning", false);
        _context._Animator.Play("Knockback", 0, 0f);
        _context._Animator.SetBool("IsKnockback", true);
        //FModAudioManager.instance.PlaySoundByName("knockback");
        ApplyKnockback();
    }

    public override void UpdateState()
    {
        BackOnGround();
    }

    public override void ExitState()
    {
        _context._Animator.SetBool("IsKnockback", false);
        if (_Stats._IsDead)
        {
            //FModAudioManager.instance.PlaySoundByName("die");
            _context.gameObject.SetActive(false);
            _context.StartCoroutine(ExecuteAfterOneFrame());
            
        }
        else
        {
            //reset _KnockBackHealth
            _context.UpdateCurrentState(CpuStateManager.State.Move);
        }
    }

    IEnumerator ExecuteAfterOneFrame()
    {
        _context._Animator.WriteDefaultValues();
        yield return null; 

        Object.Destroy(_context.gameObject);
    }

    void ApplyKnockback()
    {
        if (rb != null)
        {
            float knockbackDir = _context._Stats._Enemy ? 1f : -1f;
            
            // Use the angle from our new physics-based stats
            float angleRad = _context._ScrStats._KnockBackAngle * Mathf.Deg2Rad;
            float force = _context._ScrStats._KnockBackVelocity;

            // Calculate X and Y force based on the angle
            float forceX = Mathf.Cos(angleRad) * force * knockbackDir;
            float forceY = Mathf.Sin(angleRad) * force;

            Vector2 knockbackVector = new Vector2(forceX, forceY);
            
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackVector, ForceMode2D.Impulse);
            _Knocked = true;
        }
    }

    void BackOnGround()
    {
        if (!_Knocked) return;
        
        if (rb.linearVelocity.y > 0) return;
        
        Collider2D collider = rb.GetComponent<Collider2D>();
        if (collider == null) return;
        
        Vector2 rayOrigin = new Vector2(collider.bounds.center.x, collider.bounds.min.y);
        float rayDistance = collider.bounds.extents.y + 0.1f;
        
        RaycastHit2D groundCheck = Physics2D.Raycast(rayOrigin, Vector2.down, rayDistance, LayerMask.GetMask("Ground/TopLane", "Ground/MidLane", "Ground/BotLane"));
        
        if (groundCheck.collider != null)
        {
            ExitState();
        }
    }
}

