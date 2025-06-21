using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    public Stats playerStats;
    [SerializeField] Rigidbody2D _rb;
    PlayerBaseState _currentState;
    PlayerStateFactory _states;

    bool _attackIsPressed;
    bool _movementIsPressed;
    float _movementContext;
    bool _isCliming;
    bool _isKnockedBack;


    // getters andf setters
    public PlayerBaseState CurrentState { get { return _currentState; } set { _currentState = value; } }
    public bool IsAttackPressed { get { return _attackIsPressed; } }
    public bool IsMovementPressed { get { return _movementIsPressed; } }
    public float MovementContext { get { return _movementContext; } }
    public bool IsClimbing { get { return _isCliming; } }
    public bool IsKnockedBack { get { return _isKnockedBack; } }
    public Stats PlayerStats { get { return playerStats; } }
    public Rigidbody2D Rb { get { return _rb; } }

    //all player input callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        _movementContext = input.x;
        _movementIsPressed = input != Vector2.zero;
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        _attackIsPressed = context.ReadValueAsButton();
    }

    //all referance veraibles, player input callbacks
    void Awake()
    {
        //setup state
        _states = new PlayerStateFactory(this);
        _currentState = _states.Control();
        _currentState.EnterState();
    }

    void OnEnable()
    {
        // Enable character controls action map
    }

    void OnDisable()
    {
        // Disable character controls action map
    }

    void Update()
    {
        _currentState.UpdateStates();
    }
}
