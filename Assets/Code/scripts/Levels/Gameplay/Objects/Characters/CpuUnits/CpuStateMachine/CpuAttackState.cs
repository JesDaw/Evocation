using System.Collections.Generic;
using UnityEngine;

public class CpuAttackState : CpuBaseState
{
    //this shi feels way too complicated but i'm keeping it
    float _timer = 0f;
    enum AttackPhase { Startup, Damage, Cooldown, Done }
    AttackPhase _phase = AttackPhase.Startup;
    AttackType currentAttackType;

    public CpuAttackState(CpuStateManager context) : base(context)
    {
        _context = context;
    }

    public override void EnterState()
    {
        _context._Animator.SetBool("IsAttacking", true);
        currentAttackType = _context._ScrStats._AttackType;
        if(currentAttackType == null) Debug.Log("<color=yellow> No AttackType on cpu<color>");

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
        // Convert deltaTime to milliseconds since your logic uses 1000 multiplier
        _timer += deltaTime * 1000;

        switch (_phase)
        {
            case AttackPhase.Startup:
                // Use the range defined in the unit's stats
                currentAttackType.boxSize = _context._Stats._AttackRange;

                if (_context._AnimatorController.ShouldAttack())
                {
                    currentAttackType.Attack(_context);
                    _timer = 0f;
                    _phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                _context._Animator.SetBool("IsAttacking", false);
                if (_timer >= _context._Stats._AttackEndlag * 1000) 
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
