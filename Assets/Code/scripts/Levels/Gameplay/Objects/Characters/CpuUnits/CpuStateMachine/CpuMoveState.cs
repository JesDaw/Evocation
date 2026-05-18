using UnityEngine;
using System.Collections.Generic;

public class CpuMoveState : CpuBaseState
{
    Rigidbody2D _Body;
    Stats _Stats;
    Transform _Transform;

    public CpuMoveState(CpuStateManager context) : base(context)
    {
        _context = context;
        _Body = _context._Body;
        _Stats = _context._Stats;
        _Transform = _context.transform;
    }

    public override void EnterState()
    {
        bool facingLeft = _Transform.right.x < 0;

        bool hasTargetInRange = false;
        for (int i = 0; i < _Stats._CombatActions.Count; i++)
        {
            if (_Stats._ActionCooldownTimers[i] > 0f) continue;
            List<Stats> targets = GetTargetsForAction(_Stats._CombatActions[i], facingLeft);
            if (targets.Count > 0) { hasTargetInRange = true; break; }
        }

        _context._Animator.SetBool("IsRunning", hasTargetInRange);
        _context._Animator.SetFloat("RunningSpeed", _context._ScrStats._AnimationMoveSpeed);
        Debug.Log($"[CPU] {_context.gameObject.name}: Entered Move state, hasTarget={hasTargetInRange}");
    }

    public override void UpdateState() => Moving();

    public override void ExitState()
    {
        _context._Animator.SetBool("IsRunning", false);
        _context.UpdateCurrentState(CpuStateManager.State.CombatAction);
        Debug.Log($"[CPU] {_context.gameObject.name}: Exiting Move state");
    }

    void Moving()
    {
        _Stats.TickActionCooldowns(Time.deltaTime);
        bool facingLeft = _Transform.right.x < 0;

        for (int i = 0; i < _Stats._CombatActions.Count; i++)
        {
            CombatAction action = _Stats._CombatActions[i];

            if (_Stats._ActionCooldownTimers[i] > 0f) continue;

            List<Stats> targets = GetTargetsForAction(action, facingLeft);
            if (targets.Count == 0) continue;

            Stats primary = targets[0];
            if (primary == null || primary._IsDead) continue;

            if (action.targetCondition == ActionTargetCondition.NotAlreadyAffected)
                targets = FilterAlreadyEffected(targets, action);

            if (targets.Count == 0) continue;

            _context._CurrentAction = action;
            _context._CurrentActionIndex = i;
            _context._ActionTarget = targets[0];
            _Body.linearVelocity = Vector2.zero;
            _context._Animator.SetBool("IsRunning", false);
            ExitState();
            return;
        }

        for (int i = 0; i < _Stats._CombatActions.Count; i++)
        {
            CombatAction action = _Stats._CombatActions[i];
            List<Stats> targets = GetTargetsForAction(action, facingLeft);
            targets.RemoveAll(t => t == null || t._IsDead);
            if (targets.Count > 0)
            {
                _Body.linearVelocity = Vector2.zero;
                _context._Animator.SetBool("IsRunning", false);
                HandleIdle();
                return;
            }
        }

        _Body.linearVelocity = new Vector2(_Stats._MoveSpeed * _Transform.right.x, _Body.linearVelocity.y);
        _context._Animator.SetBool("IsRunning", true);
        HandleIdle();
    }

    List<Stats> GetTargetsForAction(CombatAction action, bool facingLeft)
    {
        float effectiveRange = _Stats._HorizontalRange * action.rangePercent;

        Vector2 center = action.extendsForward
            ? AttackLogic.CalculateAttackCenter(
                _Transform.position,
                facingLeft,
                new Vector2(effectiveRange, 0f))
            : (Vector2)_Transform.position;

        List<string> targetTags = CombatLogic.GetTargetTags(_Stats, action);
        List<Stats> targets = AttackDetection.FindTargetsInCircle(
            center, effectiveRange, targetTags, _Stats);

        targets.Sort((a, b) =>
            Vector2.Distance(_Transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(_Transform.position, b.transform.position)));

        //Debug.Log($"[CPU] {_context.gameObject.name}: GetTargetsForAction '{action.actionName}' tags={string.Join(",", targetTags)}, range={effectiveRange}, found={targets.Count}");

        return targets;
    }

    List<Stats> FilterAlreadyEffected(List<Stats> targets, CombatAction action)
    {
        if (action.effectsOnHit == null || action.effectsOnHit.Count == 0)
            return targets;

        List<Stats> filtered = new List<Stats>();
        foreach (Stats t in targets)
        {
            bool hasAny = false;
            foreach (var effect in action.effectsOnHit)
            {
                if (t.statusEffectManager.HasEffect(effect)) { hasAny = true; break; }
            }
            if (!hasAny) filtered.Add(t);
        }
        return filtered;
    }

    void HandleIdle()
    {
        if (_Stats._ActionCooldownTimers.Count == 0) return;

        int minIndex = 0;
        float minCooldown = _Stats._ActionCooldownTimers[0];
        for (int i = 1; i < _Stats._ActionCooldownTimers.Count; i++)
        {
            if (_Stats._ActionCooldownTimers[i] < minCooldown)
            {
                minCooldown = _Stats._ActionCooldownTimers[i];
                minIndex = i;
            }
        }

        bool facingLeft = _Transform.right.x < 0;
        List<Stats> nearby = GetTargetsForAction(_Stats._CombatActions[minIndex], facingLeft);
        nearby.RemoveAll(t => t == null || t._IsDead);

        if (nearby.Count > 0 && _Stats._ActionCooldownTimers[minIndex] > 0f)
        {
            _context._Animator.SetBool("IsRunning", false);
            Debug.Log($"[CPU] {_context.gameObject.name}: HandleIdle - cooldown active ({minCooldown:F1}s), in range of target");
        }
    }

    void ApplyMinStoppingDistance()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_Transform.position, 1f);
        foreach (var hit in hits)
        {
            Stats other = hit.GetComponent<Stats>();
            if (other == null || other == _Stats) continue;
            if (other._Enemy == _Stats._Enemy) continue;
            Debug.Log($"[CPU] {_context.gameObject.name}: ApplyMinStoppingDistance hit {other.gameObject.name}");
        }
    }
}