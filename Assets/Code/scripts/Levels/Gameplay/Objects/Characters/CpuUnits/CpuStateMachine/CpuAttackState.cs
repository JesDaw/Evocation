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
        _context._Animator.SetBool("IsAttacking", true);

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
                    TriesToHit();
                    _timer = 0f;
                    _phase = AttackPhase.Cooldown;
                }
                break;

            case AttackPhase.Cooldown:
                _context._Animator.SetBool("IsAttacking", false);
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

    void TriesToHit()
    {
        // if special attack types
        AttackType currentAttack = _context._ScrStats._attackType;
        switch (currentAttack)
        {
            case AOEAttackType aoeAttack:
                AOEAttack(aoeAttack.sizeX, aoeAttack.sizeY);
                return;
            default:
                DealDamage();
                break;
        }

    }

    void DealDamage()
    {
        DamageSource _damageSource = new DamageSource();
        _damageSource.IsEnemy = _context._Stats._Enemy;

        _context._AttackingStats.TakeDamage(_context._Stats._AttackDamage, _damageSource);

        List<StatusEffect> _EffectsToApply = _context._ScrStats._EffectsToApply;
        if (_EffectsToApply.Count == 0) return;
        for (int I = 0; I < _EffectsToApply.Count; I++)
        {
            _context._AttackingStats.AddStatusEffect(_EffectsToApply[I]);
        }
    }

    void AOEAttack(float sizeX, float sizeY)
    {
        Debug.Log("AOE Attack");
        //debug
        sizeX += _context._Stats._StopDistance;
        sizeX = _context._Stats._Enemy ?  -sizeX : sizeX;
        var rect = new Rect(_context.transform.position.x, _context.transform.position.y, sizeX, sizeY);
        Debug.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x + rect.width, rect.y), Color.red, 1f);
        Debug.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x, rect.y + rect.height), Color.red, 1f);
        Debug.DrawLine(new Vector3(rect.x + rect.width, rect.y + rect.height), new Vector3(rect.x + rect.width, rect.y), Color.red, 1f);
        Debug.DrawLine(new Vector3(rect.x + rect.width, rect.y + rect.height), new Vector3(rect.x, rect.y + rect.height), Color.red, 1f);

            Vector2 center = new Vector2(
            _context.transform.position.x + sizeX / 2f,
            _context.transform.position.y + sizeY / 2f
        );
        //stolen from cpuMoveState kina dubpulicated but should be fine
        Vector2 size = new Vector2(Mathf.Abs(sizeX), Mathf.Abs(sizeY));

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        for (int I = 0; I < _context._Stats._CpuPriority.Count; I++)
        {
            for (int II = 0; II < hits.Length; II++)
            {
                if (hits[II].CompareTag(_context._Stats._CpuPriority[I].ToString()))
                {
                    //actual attackers
                    GameObject EnemyGameobject = hits[II].gameObject;
                    _context._AttackingStats = EnemyGameobject.GetComponent<Stats>();
                    if (_context._AttackingStats == null) Debug.LogWarning("Make sure the enemy has their collider and stats script on the same object");
                    //this section is for healing

                    if (_context._AttackingStats._CurrentHealth >= _context._AttackingStats._MaxHealth &&
                        _context._Stats._AttackDamage <= 0
                        )
                    {
                        return;
                    }


                    if (_context._AttackingStats == null)
                    {
                        Debug.Log("No stats object attached");
                        continue;
                    }

                    DealDamage();
                }
            }
        }
    }
}
