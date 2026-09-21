using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : PlayerBaseState
{
    bool _attackOver = false;
    float _timer = 0f;
    enum AttackPhase { Startup, Cooldown, AnimationFinished, Done }
    AttackPhase _phase = AttackPhase.Startup;

    // Resolved once in EnterState — can't shift mid-execution
    CombatAction _action;
    Stats _target;

    public PlayerAttackState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
        : base(currentContext, playerStateFactory)
    {
        IsRootState = true;
    }

    public override void EnterState()
    {
        _attackOver = false;
        Ctx.PlayerCommander.TakePendingCmd(DiscretePlayerCommand.Attack);
        Ctx.Animator.SetBool("IsAttacking", true);
        _phase = AttackPhase.Startup;
        _timer = 0f;

        _action = ResolveAction();
        _target = _action != null ? FindClosestTarget(_action) : null;
    }

    public override void UpdateState()
    {
        _timer += Time.deltaTime * 1000f;

        switch (_phase)
        {
            case AttackPhase.Startup:
                if (Ctx.AnimatorController.ShouldAttack())
                {
                    if (_action != null && _target != null && !_target._IsDead)
                    {
                        CombatLogic.ExecuteAction(Ctx.PlayerStats, _action, _target);

                        if (Ctx.PlayerStats._ActionCooldownTimers != null &&
                            Ctx.PlayerStats._ActionCooldownTimers.Count > 0)
                        {
                            Ctx.PlayerStats._ActionCooldownTimers[0] =
                                Ctx.PlayerStats._ActionCooldown * _action.castCooldown;
                        }
                    }

                    _timer = 0f;
                    _phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                AnimatorStateInfo stateInfo = Ctx._animator.GetCurrentAnimatorStateInfo(0);
                bool animDone = stateInfo.normalizedTime >= 1f && !Ctx._animator.IsInTransition(0);
                if (animDone) _phase = AttackPhase.AnimationFinished;
                break;

            case AttackPhase.AnimationFinished:
                Ctx._animator.SetBool("IsAttacking", false);
                float endlagMs = (Ctx._playerStats._ExtraEndlag * 1000f);
                if (_timer >= endlagMs)
                    _phase = AttackPhase.Done;
                break;

            case AttackPhase.Done:
                _attackOver = true;
                break;
        }

        CheckSwitchStates();
    }

    public override void CheckSwitchStates()
    {
        if (Ctx.IsKnockedBack) SwitchState(Factory.KnockedBack());
        else if (_attackOver) SwitchState(Factory.GetNextState(Ctx.PlayerCommander));
    }

    public override void ExitState()
    {
        Ctx.Animator.SetBool("IsAttacking", false);
        Ctx.PlayerCommander.ClearPendingCmds(DiscretePlayerCommand.Attack);
    }

    public override void InitializeSubState() { }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    CombatAction ResolveAction()
    {
        var actions = Ctx.PlayerStats._CombatActions;
        if (actions == null || actions.Count == 0)
        {
            Debug.LogWarning($"[PlayerAttackState] {Ctx.gameObject.name}: No CombatActions configured in ScriptableStats.");
            return null;
        }
        return actions[0];
    }

    Stats FindClosestTarget(CombatAction action)
    {
        bool facingLeft = !Ctx.isFacingRight;
        float effectiveRange = Ctx.PlayerStats._HorizontalRange * action.rangePercent;

        Vector2 center = action.extendsForward
            ? CombatLogic.CalculateAttackCenter(
                Ctx.transform.position,
                facingLeft,
                new Vector2(effectiveRange, 0f))
            : (Vector2)Ctx.transform.position;

        List<string> targetTags = CombatLogic.GetTargetTags(Ctx.PlayerStats, action);
        List<Stats> candidates  = AttackDetection.FindTargetsInCircle(
            center, effectiveRange, targetTags, Ctx.PlayerStats);

        candidates.RemoveAll(t => t == null || t._IsDead);
        if (candidates.Count == 0) return null;

        candidates.Sort((a, b) =>
            Vector2.Distance(Ctx.transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(Ctx.transform.position, b.transform.position)));

        return candidates[0];
    }
}