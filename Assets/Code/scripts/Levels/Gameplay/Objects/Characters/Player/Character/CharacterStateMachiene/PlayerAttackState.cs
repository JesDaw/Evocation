using UnityEngine;
using System.Collections;

public class PlayerAttackState : PlayerBaseState
{
    //Settings
    float framesPerSecond = 60f;
    public Transform attackPoint;
    public LayerMask enemyLayers;

    // Effects
    public Animator animator;
    [SerializeField] AudioSource attackingAudio;

    public PlayerAttackState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) : base(currentContext, playerStateFactory) { }
    public override void UpdateState()
    {
        CheckSwitchStates();
    }
    public override void CheckSwitchStates()
    {
        // this if for conditions that have to be met to switch the state, doesnt apply to attacking
    }


    public override void EnterState()
    {
        HandleAttack();
        SwitchState(Factory.Idle());
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

        yield return new WaitForSeconds(Ctx.PlayerStats._AttackStartup / framesPerSecond);

        // Active hit
        AttackActive();
        attackingAudio.Play();

        // Endlag
        yield return new WaitForSeconds(Ctx.PlayerStats._AttackEndlag / framesPerSecond);
    }
    void AttackActive()
    {
        // animator.SetTrigger("Attack");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, Ctx.PlayerStats._StopDistance, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<Stats>(out Stats enemyStats))
            {
                enemyStats.Attack(Ctx.PlayerStats._AttackDamage);
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
