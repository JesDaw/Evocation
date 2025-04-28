using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    public bool controllable = true;
    [SerializeField] Stats playerStats;
    [SerializeField] float framesPerSecond = 60f;
    public Animator animator;
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [SerializeField] AudioSource attackingAudio;
    bool isAttacking = false;

    public void AttackAction(InputAction.CallbackContext context)
    {
        if (context.performed && controllable)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // Startup
        yield return new WaitForSeconds(playerStats._AttackStartup/framesPerSecond);

        // Active hit
        AttackActive();
        attackingAudio.Play();

        // Endlag
        yield return new WaitForSeconds(playerStats._AttackEndlag/framesPerSecond);

        isAttacking = false;
    }

    void AttackActive()
    {
        // animator.SetTrigger("Attack");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, playerStats._StopDistance, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<Stats>(out Stats enemyStats))
            {
                enemyStats.Attack(playerStats._AttackDamage);
            }
            else if (enemy.TryGetComponent<BuildingHealth>(out BuildingHealth buildingHealth))
            {
                buildingHealth.TakeDamage(playerStats._AttackDamage);
            }
            else
            {
                Debug.LogError(enemy.name + " is missing Stats or BuildingHealth component!");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.DrawWireSphere(attackPoint.position, playerStats._StopDistance);
    }
}
