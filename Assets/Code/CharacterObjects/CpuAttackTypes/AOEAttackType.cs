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
        Debug.Log("AOE Attack");
        //debug
        sizeX += _StopDistance;
        sizeX = _context._Stats._Enemy ?  -sizeX : sizeX;
        var rect = new Rect(_context.transform.position.x, _context.transform.position.y, sizeX, sizeY);
        Debug.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x + rect.width, rect.y), Color.red, 1f);
        Debug.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x, rect.y + rect.height), Color.red, 1f);
        Debug.DrawLine(new Vector3(rect.x + rect.width, rect.y + rect.height), new Vector3(rect.x + rect.width, rect.y), Color.red, 1f);
        Debug.DrawLine(new Vector3(rect.x + rect.width, rect.y + rect.height), new Vector3(rect.x, rect.y + rect.height), Color.red, 1f);

            Vector2 center = new Vector2(
            _context.transform.position.x + sizeX / 2f,
            _context.transform.position.y + sizeY / 2f
        );
        //stolen from cpuMoveState kina dubpulicated but should be fine
        Vector2 size = new Vector2(Mathf.Abs(sizeX), Mathf.Abs(sizeY));

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        for (int I = 0; I < _context._Stats._CpuPriority.Count; I++)
        {
            for (int II = 0; II < hits.Length; II++)
            {
                if (hits[II].CompareTag(_context._Stats._CpuPriority[I].ToString()))
                {
                    //actual attackers
                    GameObject EnemyGameobject = hits[II].gameObject;
                    _context._AttackingStats = EnemyGameobject.GetComponent<Stats>();
                    if (_context._AttackingStats == null) Debug.LogWarning("Make sure the enemy has their collider and stats script on the same object");
                    //this section is for healing

                    if (_context._AttackingStats == null)
                    {
                        Debug.Log("No stats object attached");
                        continue;
                    }

                    DealDamage(_context);
                }
            }
        }
    }
}
