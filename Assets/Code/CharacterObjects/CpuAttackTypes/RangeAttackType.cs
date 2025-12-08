using UnityEngine;

[CreateAssetMenu(fileName = "RangeAttack", menuName = "AttackType/RangeAttack")]
public class RangeAttackType : AttackType
{
    public Sprite attackApperance;
    public AnimationCurve projectileCurve;
    public float speed;
    public float offset = 2;
    public GameObject projObject;

    public override void Attack(CpuStateManager _context)
    {
        // this is for helping me visualize the attack area
        DrawCircle(_context.transform.position, _AttackRange, Color.red);
        
        GameObject createdProj = Instantiate(projObject);

        Projectile projectile = createdProj.GetComponent<Projectile>();
        projectile?.Launch(
            _context.transform.position,
            _context._AttackingStats.transform,
            projectileCurve,
            speed,
            offset,
            () => DealDamage(_context));
    }

    public override void Attack(PlayerStateMachine _context) // for Cpus enemies are declared in the movestate so the player needs to declare them here
    {
        // this is for helping me visualize the attack area
        DrawCircle(_context.transform.position, _AttackRange, Color.blue);
        
        Vector2 attackPosition = _context.transform.position;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPosition, _AttackRange);

        Transform targetTransform = null;
        Stats targetStats = null;

        for (int I = 0; I < _context.PlayerStats._CpuPriority.Count; I++)
        {
            for (int II = 0; II < hits.Length; II++)
            {
                if (hits[II].CompareTag(_context.PlayerStats._CpuPriority[I].ToString()))
                {
                    // Found a valid target
                    targetStats = hits[II].gameObject.GetComponent<Stats>();
                    if (targetStats == null)
                    {
                        Debug.LogWarning("Target missing Stats component: " + hits[II].name);
                        continue;
                    }

                    targetTransform = hits[II].transform;
                    _context._AttackingStats = targetStats;
                    break;
                }
            }
            if (targetTransform != null) break;
        }

        if (targetTransform == null)
        {
            Debug.Log("No valid targets in range for projectile");
            return;
        }

        // we should have projectiles spawn regaudless of if there is an enemy there or not i think
        GameObject createdProj = Instantiate(projObject);
        Projectile projectile = createdProj.GetComponent<Projectile>();
        projectile?.Launch(
            _context.transform.position,
            targetTransform,
            projectileCurve,
            speed,
            offset,
            () => DealDamage(_context)
        );
    }
    
    void DrawCircle(Vector3 center, float radius, Color color)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);
            
            Debug.DrawLine(point1, point2, color, 1f);
        }
    }
}