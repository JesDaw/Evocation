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
        _timer += deltaTime * 1000;

        switch (_phase)
        {
            case AttackPhase.Startup:
                if (_context._AnimatorController.ShouldAttack())
                {
                    currentAttackType.Attack(_context);

                    _timer = 0f;
                    _phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                _context._Animator.SetBool("IsAttacking", false);
                if (_timer >= currentAttackType._AttackEndlag)
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
