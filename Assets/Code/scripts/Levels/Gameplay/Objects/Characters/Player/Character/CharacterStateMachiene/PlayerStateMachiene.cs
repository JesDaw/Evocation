using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    public Stats playerStats;
    PlayerBaseState _currentState;
    PlayerStateFactory _states;

    bool _attackIsPressed;

    public PlayerBaseState CurrentState { get { return _currentState; } set { _currentState = value; } }
    public bool IsAttackPressed{ get { return _attackIsPressed; }}
    public Stats PlayerStats { get { return playerStats; }}

    //all player input callbacks
    public void Move(InputAction.CallbackContext context)
    {

    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        _attackIsPressed = context.ReadValueAsButton();
    }

    //all referance veraibles, player input callbacks
    void Awake()
    {
        _states = new PlayerStateFactory(this);
        _currentState = _states.Auto();
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
}
