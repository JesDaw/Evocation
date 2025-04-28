using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public bool _controllable = true;
    [SerializeField] Stats _player_Stats;
    public Rigidbody2D rb;
    public Transform groundCheck;
    public LayerMask groundLayer;

    private float horizontal;
    private float vertical;
    private InputAction moveActions;
    public float distance;
    public LayerMask whatIsLadder;
    private bool isFacingRight = true;
    public bool isClimbing = false;
    bool _game_is_active;

    [SerializeField] AudioSource walking_audio;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        moveActions = InputSystem.actions.FindAction("Move");
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    public void ToggleClimbing()
    {
        isClimbing = !isClimbing;
    }

    public void Move(InputAction.CallbackContext context)
    {
        if(!_controllable) return;
 
        Vector2 input = context.ReadValue<Vector2>();
        horizontal = input.x;
        vertical = input.y;
        if (!walking_audio.isPlaying && input.x != 0 && !isClimbing)
        {
            walking_audio.Play();
        }
        if (context.canceled)
        {
            walking_audio.Stop();
        }
    }

    void FixedUpdate()
    {
        if (!isClimbing)
        {
            rb.linearVelocity = new Vector2(horizontal * _player_Stats._MoveSpeed, rb.linearVelocity.y);
        }

        RaycastHit2D hitInfo = Physics2D.Raycast(transform.position, Vector2.up, distance, whatIsLadder);

        //isClimbing = hitInfo.collider != null && (isClimbing || vertical > 0);

        if (isClimbing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vertical * _player_Stats._MoveSpeed);
            rb.gravityScale = 0;

            isClimbing = hitInfo.collider != null;
        }
        else
        {
            rb.gravityScale = 4;
        }

        if (!isFacingRight && horizontal > 0f) Flip();
        else if (isFacingRight && horizontal < 0f) Flip();
    }
}
