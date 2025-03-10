using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
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
    private bool isClimbing = false;
    bool _game_is_active;

    //for walking sound effect
    private AudioManager audio_manager;
    private bool walking = false;

    //Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        moveActions = InputSystem.actions.FindAction("Move");
        audio_manager = FindAnyObjectByType<AudioManager>();
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    //for walking sound effect
    public void stop_walking()
    {
        audio_manager.Stop("Walking");
        walking = false;
    }

    public void Move(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        horizontal = input.x;
        vertical = input.y;

        if (input.magnitude > 0) //player is moving
        {
            if (!walking)
            {
                audio_manager.Play("Walking");
                walking = true;
            }
        }
        else
        {
            stop_walking();
        }
    }

    void FixedUpdate()
    {
        if (!isClimbing)
        {
            rb.linearVelocity = new Vector2(horizontal * _player_Stats._Speed, rb.linearVelocity.y);
        }

        RaycastHit2D hitInfo = Physics2D.Raycast(transform.position, Vector2.up, distance, whatIsLadder);

        isClimbing = hitInfo.collider != null && (isClimbing || vertical > 0);

        if (isClimbing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vertical * _player_Stats._Speed);
            rb.gravityScale = 0;
        }
        else
        {
            rb.gravityScale = 4;
        }

        if (!isFacingRight && horizontal > 0f) Flip();
        else if (isFacingRight && horizontal < 0f) Flip();
    }
}
