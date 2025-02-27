using UnityEngine;

public class ApplyForce : MonoBehaviour
{
    [SerializeField] Rigidbody2D _body;
    [Header("Settings")]
    [SerializeField] bool _yLock;
    [SerializeField] bool _xLock;

    public void ApplyForceToRigidbody(Vector2 force2D)
    {
        if(_yLock) force2D = new Vector2(force2D.x, 0);
        if(_xLock) force2D = new Vector2(0, force2D.y);
        _body.AddForce(force2D, ForceMode2D.Impulse);
    }
}
