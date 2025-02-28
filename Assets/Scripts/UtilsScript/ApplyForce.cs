using UnityEngine;

public class ApplyForce : MonoBehaviour
{
    [SerializeField] Rigidbody2D _body;

    public void ApplyForceToRigidbody(Vector2 force2D)
    {
        _body.AddForce(force2D, ForceMode2D.Impulse);
    }
}
