using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] Stats _playerStats;
    [SerializeField] Rigidbody2D _rb;
    [Header("Animation")]
    [SerializeField] AnimationEventsController _animatorController;
    [SerializeField] Animator _animator;
    [Header("Debug")]
    public bool DebugLogs = false;
    
    [HideInInspector] public Stats _AttackingStats;

    private bool _isActive = false;
    PlayerBaseState _currentState;
    PlayerStateFactory _states;
    PlayerCommander _commander;
    int playerId;

    //==========================================getters and setters=================================================
    public ScriptableStats ScrStats { get { return _playerStats.scriptableStats; } }
    public Stats PlayerStats { get { return _playerStats; } }
    public Animator Animator { get { return _animator; } }
    public AnimationEventsController AnimatorController { get { return _animatorController; } }
    public PlayerCommander PlayerCommander { get { return _commander; } }
    public Rigidbody2D Rb { get { return _rb; } }
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
    [HideInInspector]
    public bool isFacingRight = true;

    void Awake()
    {
        _states = new PlayerStateFactory(this);
        _currentState = _states.Idle();
        _currentState.EnterState();
    }

    void Start()
    {
        FindFreeCam();
        InitializePlayerStats();
        
         if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }
    }

    void InitializePlayerStats()
    {
        if (_playerStats.scriptableStats == null)
        {
            Debug.LogWarning("[Player state machiene] No ScriptableStats assigned to player!");
            return;
        }

        _playerStats._Enemy = false;
        _playerStats.SetTag("Player");
        
        _playerStats.targetTags.Clear();
        _playerStats.AddTargetTag("Enemy");
        
        _playerStats.InitializeStats();
    }

    void OnEnable()
    {
  
    }

    void OnDisable()
    {
        UnsubscribeFromInputs();
    }

    void SubscribeToInputs()
    {
        if (GlobalInputManager.Instance == null) 
        {
            Debug.LogWarning("[Player state machiene] Player cant find the GlobalInputManager");
            return;
        }

        var playerActions = GlobalInputManager.Instance.InputActions.Player;
        
        playerActions.Move.performed += OnMove;
        playerActions.Move.canceled += OnMove;
        playerActions.Attack.performed += OnAttack;
        if (DebugLogs) Debug.Log($"[Player state machiene] player character subscribed to inputs");
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var playerActions = GlobalInputManager.Instance.InputActions.Player;
        
        playerActions.Move.performed -= OnMove;
        playerActions.Move.canceled -= OnMove;
        playerActions.Attack.performed -= OnAttack;
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
        _currentState.UpdateStates();
    }

    public void UpdateCurrentStateToKnockback()
    {
        _currentState = _states.KnockedBack();
        _currentState.EnterState();
    }

    public void SetActive(bool active)
    {
        _isActive = active;
        
        if (active)
        {
            // When activating, check if any continuous inputs are currently held
            SyncContinuousInputs();
        }
        else
        {
            // When deactivating, clear all commands
            if (_commander != null)
            {
                _commander.ClearAllCommands();
            }
        }
    }

    /// <summary>
    /// Syncs the commander with the current state of continuous input actions
    /// This ensures held inputs are recognized when switching to this character
    /// </summary>
    void SyncContinuousInputs()
    {
        if (GlobalInputManager.Instance == null || _commander == null) return;

        var playerActions = GlobalInputManager.Instance.InputActions.Player;
        
        // Check if Move is currently pressed
        Vector2 moveValue = playerActions.Move.ReadValue<Vector2>();
        if (moveValue != Vector2.zero)
        {
            _commander.SetActiveCmd(
                ContinuousPlayerCommand.Move,
                true,
                new PlayerCommandData(moveValue)
            );
            if (DebugLogs) Debug.Log($"[Player state machiene] Synced Move input on activation: {moveValue}");
        }
    }

    void replaceAnimation()
    {
        Transform _Rig = transform.Find("Appearance")?.Find("Rig");
        if (_Rig == null || ScrStats._animator == null)
        {
            Debug.LogWarning("[Player state machiene] No Player Rig!! (for animation)");
            return;
        }

        for (int i = 0; i < ScrStats._Sprites.Length; ++i)
        {
            var spriteData = ScrStats._Sprites[i];
            string rigName = null;

            switch (spriteData.Key)
            {
                case animationRigs.animationKey.Idle: rigName = "IdleRig"; break;
                case animationRigs.animationKey.Running: rigName = "RunningRig"; break;
                case animationRigs.animationKey.Knockback: rigName = "KnockbackRig"; break;
                case animationRigs.animationKey.Attack: rigName = "AttackingRig"; break;
                default: continue;
            }

            var existing = _Rig.Find(rigName);
            if (existing != null)
                Destroy(existing.gameObject);

            spriteData.Rig.transform.position = new Vector3(
                spriteData.Offset.x,
                spriteData.Offset.y,
                spriteData.Rig.transform.position.z
            );

            spriteData.Rig.transform.rotation = Quaternion.Euler(0, 180, 0);

            GameObject newRig = Instantiate(spriteData.Rig, _Rig);
            newRig.name = rigName;

            if(rigName != "RunningRig") newRig.SetActive(false);
        }

        Animator.runtimeAnimatorController = ScrStats._animator;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (DebugLogs) Debug.Log($"[Player state machiene] State Machine Move Received _isActive: {_isActive}, free cam active: {CameraControlSwitcher.Instance.FreeCamIsActive}");
        if (!_isActive || CameraControlSwitcher.Instance.FreeCamIsActive) return; 
        _commander.OnMove(context);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!_isActive || CameraControlSwitcher.Instance.FreeCamIsActive) return; 
        _commander.OnAttack(context);
    }
}