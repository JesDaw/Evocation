using UnityEngine;

[CreateAssetMenu(fileName = "AOEAttack", menuName = "AttackType/AOEAttack")]
public class AOEAttackType : AttackType
{
    public float _SizeX;
    public float _SizeY;

    public override void Attack(CpuStateManager _context)
    {
        float sizeX = _SizeX;
        float sizeY = _SizeY;
        Debug.Log("CPU AOE Attack");
        
        sizeX += _StopDistance;
        sizeX = _context._Stats._Enemy ? -sizeX : sizeX;
        
        var rect = new Rect(_context.transform.position.x, _context.transform.position.y, sizeX, sizeY);
        Debug.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x + rect.width, rect.y), Color.red, 1f);
        Debug.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x, rect.y + rect.height), Color.red, 1f);
        Debug.DrawLine(new Vector3(rect.x + rect.width, rect.y + rect.height), new Vector3(rect.x + rect.width, rect.y), Color.red, 1f);
        Debug.DrawLine(new Vector3(rect.x + rect.width, rect.y + rect.height), new Vector3(rect.x, rect.y + rect.height), Color.red, 1f);

        Vector2 center = new Vector2(
            _context.transform.position.x + sizeX / 2f,
            _context.transform.position.y + sizeY / 2f
        );
        
        Vector2 size = new Vector2(Mathf.Abs(sizeX), Mathf.Abs(sizeY));
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        for (int I = 0; I < _context._Stats._CpuPriority.Count; I++)
        {
            for (int II = 0; II < hits.Length; II++)
            {
                if (hits[II].CompareTag(_context._Stats._CpuPriority[I].ToString()))
                {
                    GameObject enemyGameObject = hits[II].gameObject;
                    _context._AttackingStats = enemyGameObject.GetComponent<Stats>();
                    if (_context._AttackingStats == null)
                    {
                        Debug.LogWarning("Target missing Stats component on same object as collider: " + hits[II].name);
                        continue;
                    }

                    DealDamage(_context);
                }
            }
        }
    }

    public override void Attack(PlayerStateMachine _context)
    {
        float sizeX = _SizeX;
        float sizeY = _SizeY;
        Debug.Log("Player AOE Attack");
        
        sizeX += _StopDistance;
        sizeX = !_context.isFacingRight ? -sizeX : sizeX;
        
        var rect = new Rect(_context.transform.position.x, _context.transform.position.y, sizeX, sizeY);
        Debug.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x + rect.width, rect.y), Color.blue, 1f);
        Debug.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x, rect.y + rect.height), Color.blue, 1f);
        Debug.DrawLine(new Vector3(rect.x + rect.width, rect.y + rect.height), new Vector3(rect.x + rect.width, rect.y), Color.blue, 1f);
        Debug.DrawLine(new Vector3(rect.x + rect.width, rect.y + rect.height), new Vector3(rect.x, rect.y + rect.height), Color.blue, 1f);

        Vector2 center = new Vector2(
            _context.transform.position.x + sizeX / 2f,
            _context.transform.position.y + sizeY / 2f
        );
        
        Vector2 size = new Vector2(Mathf.Abs(sizeX), Mathf.Abs(sizeY));
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        for (int I = 0; I < _context.PlayerStats._CpuPriority.Count; I++)
        {
            for (int II = 0; II < hits.Length; II++)
            {
                if (hits[II].CompareTag(_context.PlayerStats._CpuPriority[I].ToString()))
                {
                    GameObject enemyGameObject = hits[II].gameObject;
                    _context._AttackingStats = enemyGameObject.GetComponent<Stats>();
                    if (_context._AttackingStats == null)
                    {
                        Debug.LogWarning("Target missing Stats component on same object as collider: " + hits[II].name);
                        continue;
                    }

                    DealDamage(_context);
                }
            }
        }
    }
}