using System.Collections.Generic;
using UnityEngine;

public static class AttackDetection
{

    public static List<IDamageable> FindTargetsInBox(
        Vector2 center,
        Vector2 size,
        List<string> targetTags,
        Stats attacker = null)
    {
        List<IDamageable> targets = new List<IDamageable>();

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        foreach (string targetTag in targetTags)
        {
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag(targetTag))
                {
                    IDamageable targetDamageable = hit.GetComponent<IDamageable>();

                    if (targetDamageable != null && targetDamageable != (IDamageable)attacker && !targets.Contains(targetDamageable))
                    {
                        targets.Add(targetDamageable);
                    }
                }
            }
        }

        return targets;
    }

    public static List<Stats> FindTargetsInCircle(
        Vector2 center, 
        float radius, 
        List<string> targetTags,
        Stats attacker = null)
    {
        List<Stats> targets = new List<Stats>();
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        
        foreach (string targetTag in targetTags)
        {
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag(targetTag))
                {
                    Stats targetStats = hit.GetComponent<Stats>();
                    
                    if (targetStats != null && targetStats != attacker && !targets.Contains(targetStats))
                    {
                        targets.Add(targetStats);
                    }
                }
            }
        }
        
        return targets;
    }

    public static IDamageable FindClosestTarget(
        Vector2 position,
        List<IDamageable> targets)
    {
        if (targets == null || targets.Count == 0) return null;

        IDamageable closest = null;
        float closestDistance = float.MaxValue;

        foreach (IDamageable target in targets)
        {
            float distance = Vector2.Distance(position, target.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = target;
            }
        }

        return closest;
    }

    public static void DrawDebugBox(Vector2 center, Vector2 size, Color color, float duration = 0.1f)
    {
        Vector2 halfSize = size * 0.5f;
        
        Vector2 topLeft = center + new Vector2(-halfSize.x, halfSize.y);
        Vector2 topRight = center + new Vector2(halfSize.x, halfSize.y);
        Vector2 bottomLeft = center + new Vector2(-halfSize.x, -halfSize.y);
        Vector2 bottomRight = center + new Vector2(halfSize.x, -halfSize.y);
        
        Debug.DrawLine(topLeft, topRight, color, duration);
        Debug.DrawLine(topRight, bottomRight, color, duration);
        Debug.DrawLine(bottomRight, bottomLeft, color, duration);
        Debug.DrawLine(bottomLeft, topLeft, color, duration);
    }

    public static void DrawDebugCircle(Vector2 center, float radius, Color color, int segments = 32, float duration = 0.1f)
    {
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector2 point1 = center + new Vector2(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
            Vector2 point2 = center + new Vector2(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);
            
            Debug.DrawLine(point1, point2, color, duration);
        }
    }
}