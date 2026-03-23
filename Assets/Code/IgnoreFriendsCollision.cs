using UnityEngine;

public class IgnoreFriendsCollision : MonoBehaviour
{
    Rigidbody2D _body;

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Allies"))
        {
            Physics2D.IgnoreCollision(
                GetComponent<Collider2D>(),
                collision.collider
            );
        }
        else if(collision.gameObject.CompareTag("Enemy"))
        {
            Physics2D.IgnoreCollision(
                GetComponent<Collider2D>(),
                collision.collider,
                false
            );
        }
    }
}
