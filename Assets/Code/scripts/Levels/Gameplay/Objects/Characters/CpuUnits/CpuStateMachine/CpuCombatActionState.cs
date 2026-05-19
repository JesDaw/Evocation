using UnityEngine;

public class CpuCombatActionState : CpuBaseState
{
    enum Phase { Startup, Cooldown, AnimationFinished, Done }
    Phase _phase;

    float _timer;

    CombatAction _action;
    int _actionIndex;

    Rigidbody2D _body;
    bool _hasTriggeredAttack;

    public CpuCombatActionState(CpuStateManager context) : base(context)
    {
        _body = context._Body;
    }

    public override void EnterState()
    {
        _action = _context._CurrentAction;
        _actionIndex = _context._CurrentActionIndex;

        _context._Animator.SetBool("IsAttacking", true);
        _context._Animator.SetBool("IsRunning", false);

        _phase = Phase.Startup;
        _timer = 0f;
        _hasTriggeredAttack = false;
    }

    public override void UpdateState() => Tick(Time.deltaTime);

    public override void ExitState()
    {
        _context._Animator.SetBool("IsAttacking", false);
        _context._ActionTarget = null;
        _context.UpdateCurrentState(CpuStateManager.State.Move);
    }

    void Tick(float deltaTime)
    {
        _timer += deltaTime * 1000f;

        switch (_phase)
        {
            case Phase.Startup:
                if (_context._ActionTarget == null || _context._ActionTarget._IsDead)
                {
                    _timer = 0f;
                    _phase = Phase.Cooldown;
                    _context._ActionTarget = null;
                    break;
                }

                if (!_hasTriggeredAttack && _context._AnimatorController.ShouldAttack())
                {
                    _hasTriggeredAttack = true;
                    Stats target = _context._ActionTarget;

                    if (target != null && !target._IsDead)
                    {
                        bool hitSomething = CombatLogic.ExecuteAction(_context._Stats, _action, target, recheckTargets: true);

                        if (hitSomething && _actionIndex >= 0 && _actionIndex < _context._Stats._ActionCooldownTimers.Count)
                        {
                            float effectiveCooldown = _context._Stats._ActionCooldown * _action.castCooldown;
                            _context._Stats._ActionCooldownTimers[_actionIndex] = effectiveCooldown;
                        }
                    }

                    _timer = 0f;
                    _phase = Phase.Cooldown;
                }

                if (_timer > 3000f && !_hasTriggeredAttack)
                {
                    _timer = 0f;
                    _phase = Phase.Cooldown;
                }
                break;

            case Phase.Cooldown:
                AnimatorStateInfo stateInfo = _context._Animator.GetCurrentAnimatorStateInfo(0);
                bool animDone = stateInfo.normalizedTime >= 1f && !_context._Animator.IsInTransition(0);
                if (animDone) _phase = Phase.AnimationFinished;
                break;

            case Phase.AnimationFinished:
                _context._Animator.SetBool("IsAttacking", false);
                float endlagMs = (_context._Stats._ExtraEndlag * 1000f) + _context._Stats._AnimationRecoveryTime;
                if (_timer >= endlagMs)
                    _phase = Phase.Done;
                break;

            case Phase.Done:
                ExitState();
                break;
        }
    }
}