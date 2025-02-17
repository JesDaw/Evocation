using UnityEngine;

public class CpuLogic : MonoBehaviour
{
    public NpcStats Stats;
    [Header("Object's components")]
    [SerializeField] SpriteRenderer _Renderer;
    [SerializeField] Rigidbody2D _Body;
    //npc private values
    [SerializeField] int Speed;
    void Start()
    {
        _Renderer.sprite = Stats._Sprite;
    }
    void Update()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right, Stats._StopDistance);
        Debug.DrawRay(transform.position, transform.right * Stats._StopDistance, Color.red);

        if (hit.collider != null)
        {
            switch (hit.collider.tag)
            {
                case "Cpu":
                    Debug.Log("Cpu hit");
                    break;

                case "Player":
                    Debug.Log("Player hit");
                    break;

                default:
                    Debug.Log("Raycast hit object: " + hit.collider.name + " with tag: " + hit.collider.tag);
                    break;
            }
        }
    }

    void FixedUpdate()
    {
        _Body.linearVelocity = new Vector2(Stats._Speed, _Body.linearVelocity.y);
    }
}
