using System.Collections.Generic;
using UnityEngine;

public class CpuMoveState : CpuBaseState
{
    Rigidbody2D _Body;
    Stats _Stats;
    Transform _Transform;

    const float BlockingCheckDistance = 0.5f;
    const float BlockingCheckRadius = 0.3f;

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
        bool shouldStop = IsBlockedByUnit(facingLeft) || HasStoppingRangeTarget(facingLeft);
        _context._Animator.SetBool("IsRunning", !shouldStop);
        _context._Animator.SetFloat("RunningSpeed", _context._ScrStats._AnimationMoveSpeed);
    }

    public override void UpdateState() => Moving();

    public override void ExitState()
    {
        _context._Animator.SetBool("IsRunning", false);
        _context.UpdateCurrentState(CpuStateManager.State.CombatAction);
    }

    void Moving()
    {
        _Stats.TickActionCooldowns(Time.deltaTime);
        bool facingLeft = _Transform.right.x < 0;

        UpdatePlayerDangerSignal(facingLeft);

        // ── Step 1: Physical blocking ─────────────────────────────────────
        // Any unit directly ahead acts as an impassable wall regardless of abilities.
        if (IsBlockedByUnit(facingLeft))
        {
            _Body.linearVelocity = Vector2.zero;
            _context._Animator.SetBool("IsRunning", false);
            TryExecuteReadyAction(facingLeft);
            return;
        }

        // ── Step 2: Stopping range check ──────────────────────────────────
        // Defined by definesStoppingRange actions, or falls back to highest
        // priority action if none have that bool set. Cooldown is ignored here —
        // the CPU stops regardless of whether it can currently fire.
        if (HasStoppingRangeTarget(facingLeft))
        {
            _Body.linearVelocity = Vector2.zero;
            _context._Animator.SetBool("IsRunning", false);
            TryExecuteReadyAction(facingLeft);
            return;
        }

        // ── Step 3: Non-stopping action ready while moving ────────────────
        // If an action that doesn't define stopping range has targets and is
        // off cooldown, pause to execute it then resume moving afterward.
        int execIdx = GetBestExecutableAction(facingLeft);
        if (execIdx >= 0)
        {
            _Body.linearVelocity = Vector2.zero;
            _context._Animator.SetBool("IsRunning", false);
            CombatAction action = _Stats._CombatActions[execIdx];
            List<Stats> targets = GetTargetsForAction(action, facingLeft);
            targets.RemoveAll(t => t == null || t._IsDead);

            if (targets.Count > 0)
            {
                _context._CurrentAction = action;
                _context._CurrentActionIndex = execIdx;
                _context._ActionTarget = targets[0];
                ExitState();
                return;
            }
        }

        // ── Step 4: Nothing to do — move forward ──────────────────────────
        _Body.linearVelocity = new Vector2(_Stats._MoveSpeed * _Transform.right.x, _Body.linearVelocity.y);
        _context._Animator.SetBool("IsRunning", true);
    }

    // Checks every combat action's effective range (_HorizontalRange * rangePercent)
    // for a live Player-tagged target, and syncs that player's danger UI accordingly.
    // Cooldowns are ignored here — "in range" should light up the warning regardless
    // of whether the CPU can currently fire.
    void UpdatePlayerDangerSignal(bool facingLeft)
    {
        Stats playerInRange = null;

        foreach (var action in _Stats._CombatActions)
        {
            List<Stats> targets = GetTargetsForAction(action, facingLeft);
            foreach (Stats t in targets)
            {
                if (t == null || t._IsDead) continue;
                if (!t.CompareTag("Player")) continue;
                playerInRange = t;
                break;
            }
            if (playerInRange != null) break;
        }

        PlayerDangerDetector detector = null;
        if (playerInRange != null) 
        {
            playerInRange.gameObject.TryGetComponent(out detector);
        }
        
        if (detector != null) UpdateDangerSignal(detector);
        else if (_context._CurrentDangerSignal != null)
        {
            _context._CurrentDangerSignal?.SetTargeted(false);
            _context._CurrentDangerSignal = null;
        }
    }

    void UpdateDangerSignal(PlayerDangerDetector current)
    {
        if (current == _context._CurrentDangerSignal) return;

        _context._CurrentDangerSignal?.SetTargeted(false);
        current?.SetTargeted(true);
        _context._CurrentDangerSignal = current;
    }

    // Fires the highest-priority ready action if one exists. Used while stopped.
    void TryExecuteReadyAction(bool facingLeft)
    {
        int execIdx = GetBestExecutableAction(facingLeft);
        if (execIdx < 0) return;

        CombatAction action = _Stats._CombatActions[execIdx];
        List<Stats> targets = GetTargetsForAction(action, facingLeft);
        targets.RemoveAll(t => t == null || t._IsDead);
        if (targets.Count == 0) return;

        _context._CurrentAction = action;
        _context._CurrentActionIndex = execIdx;
        _context._ActionTarget = targets[0];
        ExitState();
    }

    // Checks whether targets exist within the designated stopping range.
    // If any action has definesStoppingRange = true, only those define stopping.
    // If none do, the highest priority action is used as the fallback.
    bool HasStoppingRangeTarget(bool facingLeft)
    {
        bool anyDefined = false;
        foreach (var a in _Stats._CombatActions)
            if (a.definesStoppingRange) { anyDefined = true; break; }

        if (anyDefined)
        {
            for (int i = 0; i < _Stats._CombatActions.Count; i++)
            {
                if (!_Stats._CombatActions[i].definesStoppingRange) continue;
                List<Stats> targets = GetTargetsForAction(_Stats._CombatActions[i], facingLeft);
                targets.RemoveAll(t => t == null || t._IsDead);
                if (targets.Count > 0) return true;
            }
            return false;
        }
        else
        {
            // Fallback: use the highest priority action's range
            int idx = GetHighestPriorityActionIndex();
            if (idx < 0) return false;
            List<Stats> targets = GetTargetsForAction(_Stats._CombatActions[idx], facingLeft);
            targets.RemoveAll(t => t == null || t._IsDead);
            return targets.Count > 0;
        }
    }

    // Returns the index of the action with the highest priority value.
    int GetHighestPriorityActionIndex()
    {
        int bestIdx = -1;
        int bestPriority = int.MinValue;
        for (int i = 0; i < _Stats._CombatActions.Count; i++)
        {
            if (_Stats._CombatActions[i].priority > bestPriority)
            {
                bestPriority = _Stats._CombatActions[i].priority;
                bestIdx = i;
            }
        }
        return bestIdx;
    }

    // Among off-cooldown actions with live targets, returns the highest priority one.
    int GetBestExecutableAction(bool facingLeft)
    {
        int bestIdx = -1;
        int bestPriority = int.MinValue;

        for (int i = 0; i < _Stats._CombatActions.Count; i++)
        {
            if (_Stats._ActionCooldownTimers[i] > 0f) continue;

            List<Stats> targets = GetTargetsForAction(_Stats._CombatActions[i], facingLeft);
            targets.RemoveAll(t => t == null || t._IsDead);

            if (_Stats._CombatActions[i].targetCondition == ActionTargetCondition.NotAlreadyAffected)
                targets = FilterAlreadyEffected(targets, _Stats._CombatActions[i]);

            if (targets.Count == 0) continue;

            int priority = _Stats._CombatActions[i].priority;
            if (bestIdx < 0 || priority > bestPriority)
            {
                bestIdx = i;
                bestPriority = priority;
            }
        }

        return bestIdx;
    }

    // Stops movement if any unit with a Stats component (ally or enemy) is
    // within a small radius directly ahead, treating them as a solid wall.
    bool IsBlockedByUnit(bool facingLeft)
    {
        Vector2 dir = facingLeft ? Vector2.left : Vector2.right;
        Vector2 checkPos = (Vector2)_Transform.position + dir * BlockingCheckDistance;

        string ownTag = _context.gameObject.tag;
        List<string> blockingTags = ownTag == "Enemy"
            ? new List<string> { "Player", "Allies" }
            : new List<string> { "Enemy" };

        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, BlockingCheckRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == _context.gameObject) continue;
            if (!blockingTags.Contains(hit.gameObject.tag)) continue;
            if (!hit.TryGetComponent(out Stats s) || s._IsDead) continue;
            return true;
        }
        return false;
    }

    List<Stats> GetTargetsForAction(CombatAction action, bool facingLeft)
    {
        float effectiveRange = _Stats._HorizontalRange * action.rangePercent;

        Vector2 center = action.extendsForward
            ? CombatLogic.CalculateAttackCenter(
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