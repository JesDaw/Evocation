using UnityEngine;
using System.Collections;

public class PlayerAttackState : PlayerBaseState
{
    bool _attackOver = false;
    public PlayerAttackState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory)
    {
        IsRootState = true;

    }
    public override void EnterState()
    {
        _attackOver = false;
        Debug.Log("Attacking!!!");
        HandleAttack();
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
        //I STILL NEED TO REPROGRAM THIS TO FIT WITH THE NEW ATTACK SYSTEM AHHHHHHHHHHHH;
        yield return new WaitForSeconds(10);
        _attackOver = false;

        Ctx.PlayerCommander.TakePendingCmd(DiscretePlayerCommand.Attack);
        //yield return new WaitForSeconds(Ctx.PlayerStats._AttackStartup);

        // Active hit
        AttackActive();
        Ctx.AttackingAudio.Play();

        // Endlag
        //yield return new WaitForSeconds(Ctx.PlayerStats._AttackEndlag);
        _attackOver = true;

        // If any attack commands are still in the buffer, only keep 1 of them so 
        // attacks don't pile up

      /*  if (Ctx.PlayerCommander.IsCmdPending(DiscretePlayerCommand.Attack))
        {
            Ctx.PlayerCommander.ClearPendingCmds(DiscretePlayerCommand.Attack);
            Ctx.PlayerCommander.SendCmd(DiscretePlayerCommand.Attack, null);
        } */
        Ctx.PlayerCommander.ClearPendingCmds(DiscretePlayerCommand.Attack);
    }
    void AttackActive()
    {
        // animator.SetTrigger("Attack");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(Ctx.AttackPoint.position, 0, Ctx.EnemyLayers);
        Debug.Log("AttemptedAttacked");

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<Stats>(out Stats enemyStats))
            {
                //NEED TO REPROGRAM THIS TO FIT WITHT HE NEW ATTACK SYSTEM
                //enemyStats.TakeDamage(Ctx.PlayerStats._AttackDamage);
            }
            else if (enemy.TryGetComponent<BuildingHealth>(out BuildingHealth buildingHealth))
            {
                //buildingHealth.TakeDamage(Ctx.PlayerStats._AttackDamage);
            }
            else
            {
                Debug.LogError(enemy.name + " is missing Stats or BuildingHealth component!");
            }
        }
    }
}
