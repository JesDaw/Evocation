using UnityEngine;
using System.Collections;

public class PlayerAttackState : PlayerBaseState
{
    public PlayerAttackState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory) { }
    public override void CheckSwitchStates()
    {
        throw new System.NotImplementedException();
    }

    public override void EnterState()
    {
        HandleAttack();
    }

    public override void ExitState()
    {
        throw new System.NotImplementedException();
    }

    public override void InitializeSubState()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateState()
    {
        throw new System.NotImplementedException();
    }

    void HandleAttack()
    {
        StartCoroutine(AttackRoutine());
    }
    IEnumerator AttackRoutine()
    {

        yield return new WaitForSeconds(_context.PlayerStats._AttackStartup / framesPerSecond);

        // Active hit
        AttackActive();
        attackingAudio.Play();

        // Endlag
        yield return new WaitForSeconds(_context.PlayerStats._AttackEndlag / framesPerSecond);
    }
    void AttackActive()
    {
        // animator.SetTrigger("Attack");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, _context.PlayerStats._StopDistance, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<Stats>(out Stats enemyStats))
            {
                enemyStats.Attack(_context.PlayerStats._AttackDamage);
            }
            else if (enemy.TryGetComponent<BuildingHealth>(out BuildingHealth buildingHealth))
            {
                buildingHealth.TakeDamage(_context.PlayerStats._AttackDamage);
            }
            else
            {
                Debug.LogError(enemy.name + " is missing Stats or BuildingHealth component!");
            }
        }
    }
}
