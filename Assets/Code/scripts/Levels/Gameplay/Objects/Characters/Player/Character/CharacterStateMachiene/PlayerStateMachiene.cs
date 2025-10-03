using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] Stats _playerStats;
    [SerializeField] Animator _animator;

    [SerializeField] Rigidbody2D _rb;
    [SerializeField] AudioSource _walkingAudio;

    [SerializeField] int framesPerSecond = 60;
    [SerializeField] Transform attackPoint;
    [SerializeField] LayerMask enemyLayers;
    [SerializeField] AudioSource attackingAudio;

    // states
    PlayerBaseState _currentState;
    PlayerStateFactory _states;

    PlayerCommander _commander;
    int playerId;

    void Awake()
    {
        //setup state
        FindFreeCam();
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
        //Debug.Log($"current state = {_currentState}");
        _currentState.UpdateStates();
    }

    public void FindFreeCam()
    {
        // Find the CameraControllerSwitcher script
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


    // getters andf setters
    //universal stuff 
    public Stats PlayerStats { get { return _playerStats; } }
    public Animator Animator { get { return _animator; } }

    public PlayerCommander PlayerCommander { get { return _commander; } }


    // move state
    public Rigidbody2D Rb { get { return _rb; } }
    public AudioSource WalkingAudio { get { return _walkingAudio; } }
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

    public AudioSource AttackingAudio { get { return attackingAudio; } }
    public LayerMask EnemyLayers { get { return enemyLayers; } }
    public Transform AttackPoint { get { return attackPoint; } }
    public int FPS { get { return framesPerSecond; } }
    public bool IsAttackPressed { get { return _commander.IsCmdPending(DiscretePlayerCommand.Attack); } }

    // Climb state
    public bool IsClimbing { get { return _commander.IsCmdActive(ContinuousPlayerCommand.Climb); } }

    //knockback state
    public bool IsKnockedBack { get { return _commander.IsCmdPending(DiscretePlayerCommand.KnockBack); } }

    // states
    public PlayerBaseState CurrentState { get { return _currentState; } set { _currentState = value; } }


    //all player input callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        _commander.OnMove(context);
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        _commander.OnAttack(context);
    }

 public void OnToggleFreeCam(InputAction.CallbackContext context)
    {
        _commander.OnToggleFreeCam(context);
    }

    //all referance veraibles, player input callbacks

}
