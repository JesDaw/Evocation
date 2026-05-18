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
        Debug.Log($"[CPU] {_context.gameObject.name}: Entered CombatAction state");
    }

    public override void UpdateState() => Tick(Time.deltaTime);

    public override void ExitState()
    {
        _context._Animator.SetBool("IsAttacking", false);
        _context._ActionTarget = null;
        _context.UpdateCurrentState(CpuStateManager.State.Move);
        Debug.Log($"[CPU] {_context.gameObject.name}: Exiting CombatAction state");
    }

    void Tick(float deltaTime)
    {
        _timer += deltaTime * 1000f;

        string targetName = _context._ActionTarget != null ? _context._ActionTarget.gameObject.name : "NULL";
        Debug.Log($"[CPU] {_context.gameObject.name}: CombatAction Tick: phase={_phase}, action={_action?.actionName}, target={targetName}, timerMs={_timer:F1}");

        switch (_phase)
        {
            case Phase.Startup:
                if (_context._ActionTarget == null || _context._ActionTarget._IsDead)
                {
                    Debug.Log($"[CPU] {_context.gameObject.name}: Target dead or null, skipping to Cooldown");
                    _timer = 0f;
                    _phase = Phase.Cooldown;
                    _context._ActionTarget = null;
                    break;
                }

                if (!_hasTriggeredAttack)
                {
                    if (_context._AnimatorController.ShouldAttack())
                    {
                        _hasTriggeredAttack = true;
                        Stats target = _context._ActionTarget;
                        if (target != null && !target._IsDead)
                        {
                            Debug.Log($"[CPU] {_context.gameObject.name}: ExecuteAction '{_action.actionName}' on {target.gameObject.name}");
                            CombatLogic.ExecuteAction(_context._Stats, _action, target);
                        }
                        else
                            Debug.LogWarning($"{_context.gameObject.name}: Target was null or dead when trying to execute action.");

                        if (_actionIndex >= 0 && _actionIndex < _context._Stats._ActionCooldownTimers.Count)
                        {
                            _context._Stats._ActionCooldownTimers[_actionIndex] = _action.castCooldown;
                            Debug.Log($"[CPU] {_context.gameObject.name}: Set cooldown[{_actionIndex}]={_action.castCooldown}s");
                        }

                        _timer = 0f;
                        _phase = Phase.Cooldown;
                    }

                    if (_timer > 3000f && !_hasTriggeredAttack)
                    {
                        Debug.LogWarning($"[CPU] {_context.gameObject.name}: Startup timeout (3s), forcing to Cooldown");
                        _timer = 0f;
                        _phase = Phase.Cooldown;
                    }
                }
                break;

            case Phase.Cooldown:
                AnimatorStateInfo stateInfo = _context._Animator.GetCurrentAnimatorStateInfo(0);
                bool animDone = stateInfo.normalizedTime >= 1f && !_context._Animator.IsInTransition(0);
                Debug.Log($"[CPU] {_context.gameObject.name}: Cooldown: animTime={stateInfo.normalizedTime:F2}, inTransition={_context._Animator.IsInTransition(0)}, animDone={animDone}");
                if (animDone) _phase = Phase.AnimationFinished;
                break;

            case Phase.AnimationFinished:
                _context._Animator.SetBool("IsAttacking", false);
                float endlagMs = (_context._Stats._ExtraEndlag * 1000f) + _context._Stats._AnimationRecoveryTime;
                Debug.Log($"[CPU] {_context.gameObject.name}: AnimationFinished: timerMs={_timer:F1}, endlagMs={endlagMs:F1}");
                if (_timer >= endlagMs)
                    _phase = Phase.Done;
                break;

            case Phase.Done:
                ExitState();
                break;
        }
    }
}