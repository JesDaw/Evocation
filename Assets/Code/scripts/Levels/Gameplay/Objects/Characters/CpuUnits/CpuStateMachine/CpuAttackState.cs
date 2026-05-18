using UnityEngine;

public class CpuAttackState : CpuBaseState
{
    float _timer = 0f;
    enum AttackPhase { Startup, Cooldown, AnimationFinished, Done }
    AttackPhase _phase = AttackPhase.Startup;
    Rigidbody2D _Body;

    public CpuAttackState(CpuStateManager context) : base(context)
    {
        _context = context;
        _Body = _context._Body;
    }

    public override void EnterState()
    {
        _context._Animator.SetBool("IsAttacking", true);
        _context._Animator.SetBool("IsRunning", false);
        _phase = AttackPhase.Startup;
        _timer = 0;
    }

    public override void UpdateState()
    {
        Tick(Time.deltaTime);
    }

    public override void ExitState()
    {
        _context.UpdateCurrentState(CpuStateManager.State.Move);
    }

    public void Tick(float deltaTime)
    {
        _Body.linearVelocity = new Vector2(0.0f, 0.0f);
        _timer += deltaTime * 1000;
        
        switch (_phase)
        {
            case AttackPhase.Startup:
                if (_context._AnimatorController.ShouldAttack())
                {
                    AttackLogic.ExecuteAttack(_context);
                    _timer = 0f;
                    _phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                AnimatorStateInfo stateInfo = _context._Animator.GetCurrentAnimatorStateInfo(0);
                bool animationFinished = stateInfo.normalizedTime >= 1f && !_context._Animator.IsInTransition(0);
                if (animationFinished) _phase = AttackPhase.AnimationFinished;
                break;
            case AttackPhase.AnimationFinished:
                _context._Animator.SetBool("IsAttacking", false);
                float totalEndlagMs = (_context._Stats._ExtraEndlag * 1000f) + _context._Stats._AnimationRecoveryTime;
                if (_timer >= totalEndlagMs)
                {
                    _phase = AttackPhase.Done;
                }
                break;

            case AttackPhase.Done:
                ExitState();
                break;
        }
    }
}
