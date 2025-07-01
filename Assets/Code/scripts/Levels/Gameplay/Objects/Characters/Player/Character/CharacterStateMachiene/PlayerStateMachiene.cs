using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    
    [Header("UNIVERSAIL VARIABLES")]
    [SerializeField] Stats _playerStats;
    [SerializeField] Animator _animator;
    


    [Header("MOVE STATE VARIABLES")]
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] AudioSource _walkingAudio;
    InputAction.CallbackContext _WalkbuttonContext;
    bool _movementIsPressed;
    float _movementContext;


    [Header("ATTACK STATE VARIABLES")]
    [SerializeField] int framesPerSecond = 60;
    [SerializeField] Transform attackPoint;
    [SerializeField] LayerMask enemyLayers;
    [SerializeField] AudioSource attackingAudio;
    bool _attackIsPressed;

    [Header("CLIMB STATE VARIABLES")]
    bool _isCliming;

    [Header("KNOCKBACK STATE VARIABLES")]
    bool _isKnockedBack;

    // states
    PlayerBaseState _currentState;
    PlayerStateFactory _states;
    

    // getters andf setters
    //universal stuff 
    public Stats PlayerStats { get { return _playerStats; } }
    public Animator Animator { get { return _animator; } }
    

    // move state
    public Rigidbody2D Rb { get { return _rb; } }
    public AudioSource WalkingAudio { get { return _walkingAudio; } }
    public bool IsMovementPressed { get { return _movementIsPressed; } }
    public float MovementContext { get { return _movementContext; } }
    public InputAction.CallbackContext ButtonContext { get { return _WalkbuttonContext;}}

    // attack state
    public AudioSource AttackingAudio { get { return attackingAudio; } }
    public LayerMask EnemyLayers { get { return enemyLayers; }}
    public Transform AttackPoint { get { return attackPoint;  }}
    public int FPS { get { return framesPerSecond; } }
    public bool IsAttackPressed { get { return _attackIsPressed; } }

    // Climb state
    public bool IsClimbing { get { return _isCliming; } }

    //knockback state
    public bool IsKnockedBack { get { return _isKnockedBack; } }

    // states
    public PlayerBaseState CurrentState { get { return _currentState; } set { _currentState = value; } }


    //all player input callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        _WalkbuttonContext = context;
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
        _currentState = _states.Idle();
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
        Debug.Log(_currentState);
    }
}
