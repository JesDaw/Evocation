using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] Stats _playerStats;
    [SerializeField] Animator _animator;
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] AudioSource _walkingAudio;
    [SerializeField] Transform attackPoint;
    [SerializeField] LayerMask enemyLayers;
    [SerializeField] AudioSource attackingAudio;
    [SerializeField] AttackType playerAttackType;

    // Each player has their own input instance
    public InputSystem_Actions playerInputActions;
    private bool _isActive = false;

    // states
    PlayerBaseState _currentState;
    PlayerStateFactory _states;
    PlayerCommander _commander;
    int playerId;

    //==========================================getters and setters=================================================
    public Stats PlayerStats { get { return _playerStats; } }
    public Animator Animator { get { return _animator; } }
    public PlayerCommander PlayerCommander { get { return _commander; } }
    public Rigidbody2D Rb { get { return _rb; } }
    public AudioSource WalkingAudio { get { return _walkingAudio; } }
    public AudioSource AttackingAudio { get { return attackingAudio; } }
    public LayerMask EnemyLayers { get { return enemyLayers; } }
    public Transform AttackPoint { get { return attackPoint; } }
    public int PlayerID { get; set; }
    
    public bool IsMovementPressed { get { return _commander.IsCmdActive(ContinuousPlayerCommand.Move); } }
    public float MovementContext
    {
        get
        {
            PlayerCommandData? data;
            if (_commander.IsCmdActive(ContinuousPlayerCommand.Move, out data))
            {
                return data.Value.AsVector2.Value.x;
            }
            return 0;
        }
    }
    public bool IsAttackPressed { get { return _commander.IsCmdPending(DiscretePlayerCommand.Attack); } }
    public bool IsClimbing { get { return _commander.IsCmdActive(ContinuousPlayerCommand.Climb); } }
    public bool IsKnockedBack { get { return _commander.IsCmdPending(DiscretePlayerCommand.KnockBack); } }
    public PlayerBaseState CurrentState { get { return _currentState; } set { _currentState = value; } }

    void Awake()
    {
        // Create this player's own input instance
        playerInputActions = new InputSystem_Actions();
        
        // Initialize state machine
        _states = new PlayerStateFactory(this);
        _currentState = _states.Idle();
        _currentState.EnterState();
    }

    void Start()
    {
        FindFreeCam();
    }

    void OnEnable()
    {
        // Subscribe to this player's input events
        playerInputActions.Player.Move.performed += OnMove;
        playerInputActions.Player.Move.canceled += OnMove;
        playerInputActions.Player.Attack.performed += OnAttack;
        
        // Start with inputs disabled (will be enabled when player becomes active)
        playerInputActions.Player.Disable();
    }

    void OnDisable()
    {
        // Unsubscribe when disabled
        playerInputActions.Player.Move.performed -= OnMove;
        playerInputActions.Player.Move.canceled -= OnMove;
        playerInputActions.Player.Attack.performed -= OnAttack;
        
        playerInputActions.Disable();
    }

    public void FindFreeCam()
    {
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            CameraControlSwitcher ccs = obj.GetComponent<CameraControlSwitcher>();
            if (ccs != null)
            {
                _commander = new PlayerCommander(ccs.FreeCamIsActive);
                break;
            }
        }
    }

    void Update()
    {
        // Always update the state machine - idle, knockback, etc. still need to work
        _currentState.UpdateStates();
    }

    // Called by PlayerSwitch to activate/deactivate this player's INPUTS
    public void SetActive(bool active)
    {
        _isActive = active;
        
        if (active)
        {
            playerInputActions.Player.Enable();
            //Debug.Log($"{gameObject.name} inputs ENABLED");
        }
        else
        {
            playerInputActions.Player.Disable();
            //Debug.Log($"{gameObject.name} inputs DISABLED");
        }
    }

    //==========================================Input callbacks===================================================
    // These only process input when this player is active
    public void OnMove(InputAction.CallbackContext context)
    {
        //Debug.Log($"{gameObject.name} OnMove called - Active: {_isActive}, Value: {context.ReadValue<Vector2>()}");
        if (!_isActive) return; // Block input if not active
        _commander.OnMove(context);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        //Debug.Log($"{gameObject.name} OnAttack called - Active: {_isActive}");
        if (!_isActive) return; // Block input if not active
        _commander.OnAttack(context);
    }

    public void OnToggleFreeCam(InputAction.CallbackContext context)
    {
        // Keep if needed
    }

    void OnDestroy()
    {
        // Clean up
        playerInputActions?.Dispose();
    }
}