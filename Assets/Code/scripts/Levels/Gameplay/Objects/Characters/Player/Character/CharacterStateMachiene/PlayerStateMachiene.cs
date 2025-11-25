using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] ScriptableStats _scrStats; // Use the same ScriptableStats as CPU!
    [SerializeField] Stats _playerStats;
    [SerializeField] Animator _animator;
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] AudioSource _walkingAudio;
    [SerializeField] AudioSource attackingAudio;
    [SerializeField] AnimationEventsController _animatorController;
    
    // Add this to match CPU's _AttackingStats
    [HideInInspector] public Stats _AttackingStats;

    // Each player has their own input instance
    public InputSystem_Actions playerInputActions;
    private bool _isActive = false;

    // states
    PlayerBaseState _currentState;
    PlayerStateFactory _states;
    PlayerCommander _commander;
    int playerId;

    //==========================================getters and setters=================================================
    public ScriptableStats ScrStats { get { return _scrStats; } } // Add this
    public Stats PlayerStats { get { return _playerStats; } }
    public Animator Animator { get { return _animator; } }
    public AnimationEventsController AnimatorController { get { return _animatorController; } }
    public PlayerCommander PlayerCommander { get { return _commander; } }
    public Rigidbody2D Rb { get { return _rb; } }
    public AudioSource WalkingAudio { get { return _walkingAudio; } }
    public AudioSource AttackingAudio { get { return attackingAudio; } }
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
        InitializeStatsFromScriptable(); // Initialize player stats from ScriptableStats
    }

    void InitializeStatsFromScriptable()
    {
        if (_scrStats == null)
        {
            Debug.LogWarning("No ScriptableStats assigned to player!");
            return;
        }

        // Initialize stats like CPU does in CpuStateManager.Start()
        _playerStats._MaxHealth = _scrStats._MaxHealth;
        _playerStats._CurrentHealth = _scrStats._MaxHealth;
        _playerStats._MoveSpeed = _scrStats._MoveSpeed;
        _playerStats._KnockBackHealth = _scrStats._KnockBackMax;
        _playerStats._KnockBackMax = _scrStats._KnockBackMax;
        
        // Set clan/team (players are not enemies)
        _playerStats._Enemy = false;
        _playerStats._Clan = Evocation.Clans.ClansList.Player;
        gameObject.tag = _playerStats._Clan.ToString();
        
        // Set up CPU priority for targeting
        if (!_playerStats._CpuPriority.Contains(Evocation.Clans.ClansList.Enemy))
        {
            _playerStats._CpuPriority.Insert(0, Evocation.Clans.ClansList.Enemy);
        }
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
        }
        else
        {
            playerInputActions.Player.Disable();
        }
    }

    //==========================================Input callbacks===================================================
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!_isActive) return; 
        _commander.OnMove(context);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!_isActive) return; 
        _commander.OnAttack(context);
    }

    public void OnToggleFreeCam(InputAction.CallbackContext context)
    {
        // Keep if needed
    }

    void OnDestroy()
    {
        playerInputActions?.Dispose();
    }
}