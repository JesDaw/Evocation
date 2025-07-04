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
            var nextState = Factory.GetNextState(Ctx.PlayerCommander);
            if (this.Equals(nextState))
            {
                // If we're here then there must be another attack command
                // in the queue.  We'll service it by restarting this state.
                EnterState();
            }
            else
            {
                SwitchState(nextState);
            }
        }
    }


    public override void EnterState()
    {
        _attackOver = false;
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
        _attackOver = false;

        Ctx.PlayerCommander.TakePendingCmd(DiscretePlayerCommand.Attack);
        yield return new WaitForSeconds(Ctx.PlayerStats._AttackStartup / Ctx.FPS);

        // Active hit
        AttackActive();
        Ctx.AttackingAudio.Play();

        // Endlag
        yield return new WaitForSeconds(Ctx.PlayerStats._AttackEndlag / Ctx.FPS);
        _attackOver = true;

        // If any attack commands are still in the buffer, only keep 1 of them so 
        // attacks don't pile up
        /*
        if (Ctx.PlayerCommander.IsCmdPending(DiscretePlayerCommand.Attack))
        {
            Ctx.PlayerCommander.ClearPendingCmds(DiscretePlayerCommand.Attack);
            Ctx.PlayerCommander.SendCmd(DiscretePlayerCommand.Attack, null);
        */
        Ctx.PlayerCommander.ClearPendingCmds(DiscretePlayerCommand.Attack);
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
