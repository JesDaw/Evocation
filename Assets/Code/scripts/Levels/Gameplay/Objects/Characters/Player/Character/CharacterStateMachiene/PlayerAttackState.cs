using UnityEngine;
using System.Collections;

public class PlayerAttackState : PlayerBaseState
{
    bool _attackOver = false;
   public PlayerAttackState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;

    }
    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override void CheckSwitchStates()
    {
        if (Ctx.IsKnockedBack) { SwitchState(Factory.KnockedBack()); }
        if (_attackOver)
        {
            if (Ctx.IsClimbing) { SwitchState(Factory.Climb()); }
            else if (!Ctx.IsAttackPressed && !Ctx.IsMovementPressed && !Ctx.IsClimbing && !Ctx.IsKnockedBack) { SwitchState(Factory.Idle()); }
            //else if (Ctx.IsAttackPressed) { SwitchState(Factory.Attack()); }
            else if (Ctx.IsMovementPressed) { SwitchState(Factory.Move()); }
        }
    }


    public override void EnterState()
    {
        HandleAttack();
    }

    public override void ExitState()
    {
        //if we want something to happen as the state is left
    }

    public override void InitializeSubState()
    {
        // if this gets substates
    }

    void HandleAttack()
    {
        Ctx.StartCoroutine(AttackRoutine());
    }
    IEnumerator AttackRoutine()
    {

        yield return new WaitForSeconds(Ctx.PlayerStats._AttackStartup / Ctx.FPS);

        // Active hit
        AttackActive();
        Ctx.AttackingAudio.Play();

        // Endlag
        yield return new WaitForSeconds(Ctx.PlayerStats._AttackEndlag / Ctx.FPS);
        _attackOver = true;
    }
    void AttackActive()
    {
        // animator.SetTrigger("Attack");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(Ctx.AttackPoint.position, Ctx.PlayerStats._StopDistance, Ctx.EnemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<Stats>(out Stats enemyStats))
            {
                enemyStats.TakeDamage(Ctx.PlayerStats._AttackDamage);
            }
            else if (enemy.TryGetComponent<BuildingHealth>(out BuildingHealth buildingHealth))
            {
                buildingHealth.TakeDamage(Ctx.PlayerStats._AttackDamage);
            }
            else
            {
                Debug.LogError(enemy.name + " is missing Stats or BuildingHealth component!");
            }
        }
    }
}
