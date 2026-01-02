using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] ScriptableStats _scrStats;
    [SerializeField] Stats _playerStats;
    [SerializeField] Rigidbody2D _rb;
    [Header("Audio")]
    [SerializeField] AudioSource _walkingAudio;
    [SerializeField] AudioSource attackingAudio;
    [Header("Animation")]
    [SerializeField] AnimationEventsController _animatorController;
    [SerializeField] Animator _animator;
    
    [HideInInspector] public Stats _AttackingStats;

    private bool _isActive = false;

    // states
    PlayerBaseState _currentState;
    PlayerStateFactory _states;
    PlayerCommander _commander;
    int playerId;

    //==========================================getters and setters=================================================
    public ScriptableStats ScrStats { get { return _scrStats; } }
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
    [HideInInspector]
    public bool isFacingRight = true;

    void Awake()
    {
        // Initialize state machine
        _states = new PlayerStateFactory(this);
        _currentState = _states.Idle();
        _currentState.EnterState();
    }

    void Start()
    {
        FindFreeCam();
        InitializeStatsFromScriptable();
        
        // Ensure we're subscribed (in case OnEnable happened before GlobalInputManager existed)
        SubscribeToInputs();
        
        StartCoroutine(Startup());
    }

    void InitializeStatsFromScriptable()
    {
        if (_scrStats == null)
        {
            Debug.LogWarning("No ScriptableStats assigned to player!");
            return;
        }

        _playerStats._MaxHealth = _scrStats._MaxHealth;
        _playerStats._CurrentHealth = _scrStats._MaxHealth;
        _playerStats._MoveSpeed = _scrStats._MoveSpeed;
        _playerStats._KnockBackHealth = _scrStats._KnockBackMax;
        _playerStats._KnockBackMax = _scrStats._KnockBackMax;
        
        _playerStats._Enemy = false;
        _playerStats._Clan = Evocation.Clans.ClansList.Player;
        gameObject.tag = _playerStats._Clan.ToString();
        
        if (!_playerStats._CpuPriority.Contains(Evocation.Clans.ClansList.Enemy))
        {
            _playerStats._CpuPriority.Insert(0, Evocation.Clans.ClansList.Enemy);
        }
    }

    void OnEnable()
    {
        // Subscribe to input from GlobalInputManager (with safety check)
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }
        else
        {
            // GlobalInputManager not ready yet, will subscribe in Start
            Debug.LogWarning($"GlobalInputManager not ready when {gameObject.name} enabled. Will subscribe in Start.");
        }
    }

    void OnDisable()
    {
        // Unsubscribe when disabled
        UnsubscribeFromInputs();
    }

    void SubscribeToInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var playerActions = GlobalInputManager.Instance.InputActions.Player;
        
        playerActions.Move.performed += OnMove;
        playerActions.Move.canceled += OnMove;
        playerActions.Attack.performed += OnAttack;
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
        // Always update the state machine - idle, knockback, etc. still need to work
        _currentState.UpdateStates();
    }

    public void UpdateCurrentStateToKnockback()
    {
        _currentState = _states.KnockedBack();
        _currentState.EnterState();
    }

    /// <summary>
    /// Called by PlayerSwitch to activate/deactivate this player's ability to respond to inputs
    /// Note: This doesn't enable/disable the action map, just sets a flag for this player
    /// </summary>
    public void SetActive(bool active)
    {
        _isActive = active;
        //Debug.Log($"Player {gameObject.name} set active: {active}");
    }

    //==animation replacement
    IEnumerator Startup()
    {
        yield return new WaitUntil(() => transform.childCount >= ScrStats._Sprites.Length);
        replaceAnimation();

        if(Animator != null)
        {
            Animator.Rebind();
            Animator.Update(0f);
        }
    }

    void replaceAnimation()
    {
        Transform _Rig = transform.Find("Appearance")?.Find("Rig");
        if (_Rig == null || ScrStats._animator == null)
        {
            Debug.LogWarning("No Cpu Rig!! (for animation)");
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

    //==========================================Input callbacks===================================================
    public void OnMove(InputAction.CallbackContext context)
    {
        // Only respond if this player is active
        if (!_isActive) return; 
        _commander.OnMove(context);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        // Only respond if this player is active
        if (!_isActive) return; 
        _commander.OnAttack(context);
    }
}