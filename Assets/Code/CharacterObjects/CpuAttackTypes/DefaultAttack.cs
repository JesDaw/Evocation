using UnityEngine;

[CreateAssetMenu(fileName = "DefaultAttack", menuName = "AttackType/DefaultAttack")]
public class DefaultAttackType : AttackType
{
    public override void Attack(CpuStateManager _context)
    {
        // this is for helping me visualize the attack area
        DrawCircle(_context.transform.position, _AttackRange, Color.red);
        
        DealDamage(_context);
    }

    public override void Attack(PlayerStateMachine _context)
    {
        // this is for helping me visualize the attack area
        DrawCircle(_context.transform.position, _AttackRange, Color.blue);
        
        Vector2 attackPosition = _context.transform.position;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPosition, _AttackRange);

        for (int I = 0; I < _context.PlayerStats._CpuPriority.Count; I++)
        {
            for (int II = 0; II < hits.Length; II++)
            {
                if (hits[II].CompareTag(_context.PlayerStats._CpuPriority[I].ToString()))
                {
                    // Found a valid target
                    _context._AttackingStats = hits[II].gameObject.GetComponent<Stats>();
                    if (_context._AttackingStats == null)
                    {
                        Debug.LogWarning("Target missing Stats component: " + hits[II].name);
                        continue;
                    }

                    DealDamage(_context);
                    return; // Attack first valid target found and exit
                }
            }
        }
        
        Debug.Log("No valid targets in melee range");
    }
    
    // this is for helping me visualize the attack area
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