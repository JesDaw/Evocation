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
        int candidateIdx = GetBestCandidate(false, facingLeft);

        if (candidateIdx >= 0)
        {
            List<Stats> targets = GetTargetsForAction(_Stats._CombatActions[candidateIdx], facingLeft);
            targets.RemoveAll(t => t == null || t._IsDead);
            _context._Animator.SetBool("IsRunning", targets.Count > 0);
        }
        else
        {
            _context._Animator.SetBool("IsRunning", false);
        }

        _context._Animator.SetFloat("RunningSpeed", _context._ScrStats._AnimationMoveSpeed);
    }

    public override void UpdateState() => Moving();

    public override void ExitState()
    {
        _context._Animator.SetBool("IsRunning", false);
        _context.UpdateCurrentState(CpuStateManager.State.CombatAction);
        Debug.Log($"[CPU] {_context.gameObject.name}: Exiting Move state");
    }

    int GetBestCandidate(bool onlyIfReady, bool facingLeft)
    {
        int bestIdx = -1;
        int bestTier = int.MaxValue;
        int bestPriority = int.MinValue;

        for (int i = 0; i < _Stats._CombatActions.Count; i++)
        {
            if (onlyIfReady && _Stats._ActionCooldownTimers[i] > 0f) continue;

            List<Stats> targets = GetTargetsForAction(_Stats._CombatActions[i], facingLeft);
            targets.RemoveAll(t => t == null || t._IsDead);

            if (_Stats._CombatActions[i].targetCondition == ActionTargetCondition.NotAlreadyAffected)
                targets = FilterAlreadyEffected(targets, _Stats._CombatActions[i]);

            bool hasTargets = targets.Count > 0;

            int tier;
            if (_Stats._ActionCooldownTimers[i] <= 0f)
                tier = 0;
            else if (hasTargets)
                tier = 1;
            else
                tier = 2;

            int priority = _Stats._CombatActions[i].priority;

            if (bestIdx < 0 || tier < bestTier || (tier == bestTier && priority > bestPriority))
            {
                bestIdx = i;
                bestTier = tier;
                bestPriority = priority;
            }
        }

        return bestIdx;
    }

    int GetBestExecutableAction(bool facingLeft)
    {
        int bestIdx = -1;
        int bestPriority = int.MinValue;
        float closestDist = float.MaxValue;

        for (int i = 0; i < _Stats._CombatActions.Count; i++)
        {
            if (_Stats._ActionCooldownTimers[i] > 0f) continue;

            List<Stats> targets = GetTargetsForAction(_Stats._CombatActions[i], facingLeft);
            targets.RemoveAll(t => t == null || t._IsDead);

            if (_Stats._CombatActions[i].targetCondition == ActionTargetCondition.NotAlreadyAffected)
                targets = FilterAlreadyEffected(targets, _Stats._CombatActions[i]);

            if (targets.Count == 0) continue;

            int priority = _Stats._CombatActions[i].priority;
            float dist = Vector2.Distance(_Transform.position, targets[0].transform.position);

            if (bestIdx < 0 || priority > bestPriority)
            {
                bestIdx = i;
                bestPriority = priority;
                closestDist = dist;
            }
            else if (priority == bestPriority && dist < closestDist)
            {
                bestIdx = i;
                closestDist = dist;
            }
        }

        return bestIdx;
    }

    void Moving()
    {
        _Stats.TickActionCooldowns(Time.deltaTime);
        bool facingLeft = _Transform.right.x < 0;

        int execIdx = GetBestExecutableAction(facingLeft);
        if (execIdx >= 0)
        {
            CombatAction action = _Stats._CombatActions[execIdx];
            List<Stats> targets = GetTargetsForAction(action, facingLeft);
            targets.RemoveAll(t => t == null || t._IsDead);

            _context._CurrentAction = action;
            _context._CurrentActionIndex = execIdx;
            _context._ActionTarget = targets[0];
            _Body.linearVelocity = Vector2.zero;
            _context._Animator.SetBool("IsRunning", false);
            ExitState();
            return;
        }

        int detectionIdx = GetBestCandidate(false, facingLeft);
        CombatAction detectionAction = detectionIdx >= 0 ? _Stats._CombatActions[detectionIdx] : null;
        List<Stats> detectionTargets = detectionIdx >= 0 ? GetTargetsForAction(detectionAction, facingLeft) : new List<Stats>();
        detectionTargets.RemoveAll(t => t == null || t._IsDead);

        if (detectionAction != null)
        {
            string cd1 = _Stats._ActionCooldownTimers[0].ToString("F1");
            string cd2 = _Stats._ActionCooldownTimers.Count > 1 ? _Stats._ActionCooldownTimers[1].ToString("F1") : "N/A";
            //Debug.Log($"[CPU] {_context.gameObject.name}: detection={detectionIdx}({detectionAction.actionName}) pri={detectionAction.priority}, targets={detectionTargets.Count}, cooldowns=[{cd1},{cd2}]");
        }

        if (detectionTargets.Count > 0)
        {
            _Body.linearVelocity = Vector2.zero;
            _context._Animator.SetBool("IsRunning", false);
            return;
        }

        _Body.linearVelocity = new Vector2(_Stats._MoveSpeed * _Transform.right.x, _Body.linearVelocity.y);
        _context._Animator.SetBool("IsRunning", true);
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
}