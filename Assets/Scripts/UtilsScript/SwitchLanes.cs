using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchLanes : MonoBehaviour
{
    [SerializeField] GameObject TopLane;
    [SerializeField] bool Switched;
    Camera cam;
    Collider2D myCollider;
    bool AbleToSwitch;

    void Awake()
    {
        cam = Camera.main;

        myCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        AbleToSwitch = hit.collider == myCollider;
    }

    public float switchCooldown = 1f;
    private float lastSwitchTime = 0f;

    public void InputLanes()
    {
        if (Time.time - lastSwitchTime < switchCooldown) return;

        if (!AbleToSwitch) return;

        bool x = !Switched;
        Debug.Log("Switched");
        TopLane.GetComponent<BoxCollider2D>().enabled = Switched;
        Switched = x;

        lastSwitchTime = Time.time;
    }
}
