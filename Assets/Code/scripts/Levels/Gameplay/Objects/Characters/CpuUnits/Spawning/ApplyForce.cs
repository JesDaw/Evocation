using UnityEngine;

public class ApplyForce : MonoBehaviour
{
    //whats this for is this used anywhere? I feel liek wherever its used its so simple it can just be hard coded in the places it needs to be
    [SerializeField] Rigidbody2D _body;

    public void ApplyForceToRigidbody(Vector2 force2D)
    {
        _body.AddForce(force2D, ForceMode2D.Impulse);
    }
}
