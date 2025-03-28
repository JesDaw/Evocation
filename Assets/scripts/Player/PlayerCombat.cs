using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Combat : MonoBehaviour
{
    public bool _controllable = true;
    [SerializeField] Stats _player_Stats;
    public Animator animator;

    public Transform attackPoint;
    public LayerMask enemyLayers;

    //for sword slash sound effect
    [SerializeField] AudioSource attackingAudio;

    // Update is called once per frame
    public void AttackAction(InputAction.CallbackContext context)
    {
        if (context.performed && _controllable)
        {
            Attack();
            attackingAudio.Play();
        }
    }

    void Attack()
    {
        // No Attack animation yet. Uncomment below code when animation has been implemented
        // animator.SetTrigger("Attack");

        // Detect enemies in range of attack
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, _player_Stats._StopDistance, enemyLayers);
        _player_Stats = GetComponent<Stats>();

        // Damage them
        foreach(Collider2D enemy in hitEnemies)
        {
            if (!enemy.CompareTag("foe")) continue;

            if (enemy.TryGetComponent<Stats>(out Stats _enimy_stats)){
                _enimy_stats.Attack(_player_Stats._Attack);
                }
            else
                {
                //Debug.LogError(enemy.name + " is missing the Stats component!");
                 //continue; // Skip this enemy if it doesn't have Stats
                }

            if (enemy.TryGetComponent<BuildingHealth>(out BuildingHealth _building_health)){
                _building_health.TakeDamage(_player_Stats._Attack);
                }
            else
                {
                //Debug.LogError(enemy.name + " is missing the BuildingHealth component!");
                 //continue; // Skip this enemy if it doesn't have Stats
                }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.DrawWireSphere(attackPoint.position, _player_Stats._StopDistance);
    }
}
