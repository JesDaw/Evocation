using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    public bool controllable = true;
    [SerializeField] Stats playerStats;
    //[SerializeField] float framesPerSecond = 60f;
    public Animator animator;
    public Transform attackPoint;
    public LayerMask enemyLayers;
    [SerializeField] PlayersControlerScriptsManager controlsManager;

    [SerializeField] AudioSource attackingAudio;
    //bool isAttacking = false;

    public void AttackAction(InputAction.CallbackContext context)
    {
        if (context.performed && controllable)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
       // isAttacking = true;

        // Startup
        controlsManager.DisableControls();
        //yield return new WaitForSeconds(playerStats._AttackStartup/framesPerSecond);

        // Active hit
        AttackActive();
        attackingAudio.Play();

        // Endlag
        //yield return new WaitForSeconds(playerStats._AttackEndlag/framesPerSecond);
        yield return new WaitForSeconds(10);
        controlsManager.EnableControls();

       // isAttacking = false;
    }

    void AttackActive()
    {
        // animator.SetTrigger("Attack");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, 0, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.TryGetComponent<Stats>(out Stats enemyStats))
            {
                //enemyStats.TakeDamage(playerStats._AttackDamage);
            }
            else if (enemy.TryGetComponent<BuildingHealth>(out BuildingHealth buildingHealth))
            {
                //buildingHealth.TakeDamage(playerStats._AttackDamage);
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

        //Gizmos.DrawWireSphere(attackPoint.position, playerStats._StopDistance);
    }
}
