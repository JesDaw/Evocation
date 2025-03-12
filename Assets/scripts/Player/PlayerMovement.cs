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

    //for walking sound effect to stop playing
    public void stop_walking()
    {
    //debugged error when this sound still plays when switched cam; has to check if sound exists first
        if (audio_manager != null)
        {
            audio_manager.Stop("Walking");
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        horizontal = input.x;
        vertical = input.y;

        //used to be 'input.magnitude > 0'
        if (Mathf.Abs(horizontal) > 0 && Mathf.Abs(vertical) == 0) //now walk sound only plays when player moves left/right
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

    public void EngageClimbing()
    {
        isClimbing = !isClimbing;

        if (isClimbing)
        {
            // snap position to center of ladder
            // Somehow get ladder position
        }


    }

    void FixedUpdate()
    {
        if (!isClimbing)
        {
            rb.linearVelocity = new Vector2(horizontal * _player_Stats._Speed, rb.linearVelocity.y);
        }

        RaycastHit2D hitInfo = Physics2D.Raycast(transform.position, Vector2.up, distance, whatIsLadder);

        //isClimbing = hitInfo.collider != null && (isClimbing || vertical > 0);

        if (isClimbing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vertical * _player_Stats._Speed);
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
