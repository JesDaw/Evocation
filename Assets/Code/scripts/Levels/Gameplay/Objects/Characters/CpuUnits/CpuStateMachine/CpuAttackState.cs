using System.Collections.Generic;
using UnityEngine;

public class CpuAttackState : CpuBaseState
{
    //this shi feels way too complicated but i'm keeping it
    float _timer = 0f;
    enum AttackPhase { Startup, Damage, Cooldown, Done }
    AttackPhase _phase = AttackPhase.Startup;

    public CpuAttackState(CpuStateManager context) : base(context)
    {
        _context = context;
    }

    public override void EnterState()
    {
        _context._Animator.SetBool("IsRunning", false);
        Debug.Log("Attacking " + _context._AttackingStats.gameObject.name);
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
                if (_timer >= _context._Stats._AttackStartup)
                {
                    DealDamage();
                    _timer = 0f;
                    _phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                if (_timer >= _context._Stats._AttackEndlag)
                {
                    _phase = AttackPhase.Done;
                }
                break;

            case AttackPhase.Done:
                ExitState();
                break;
        }
    }

    void DealDamage()
    {
        DamageSource _damageSource = new DamageSource();
        _damageSource.IsEnemy = _context._Stats._Enemy;

        _context._AttackingStats.TakeDamage(_context._Stats._AttackDamage, _damageSource);

        //status effects
        List<StatusEffect> _EffectsToApply = _context._ScrStats._EffectsToApply;
        if (_EffectsToApply.Count == 0) return;
        for (int I = 0; I < _EffectsToApply.Count; I++)
        {
            _context._AttackingStats.AddStatusEffect(_EffectsToApply[I]);
        }
    }
}
