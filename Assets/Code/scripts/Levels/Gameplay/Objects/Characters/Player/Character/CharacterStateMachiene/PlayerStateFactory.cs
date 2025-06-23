public class PlayerStateFactory
{
    PlayerStateMachine _context;
    public PlayerStateFactory(PlayerStateMachine CurrentContext) { _context = CurrentContext; }
    public PlayerBaseState Idle() { return new PlayerIdleState(_context, this); }
    public PlayerBaseState Move() { return new PlayerMoveState(_context, this); }
    public PlayerBaseState Attack() { return new PlayerAttackState(_context, this); }
    public PlayerBaseState KnockedBack() {return new PlayerKnockedBackState(_context, this); }
    public PlayerBaseState Climb() { return new PlayerClimbState(_context, this); }
    public PlayerBaseState Control() {return new PlayerControlState(_context, this); }
    public PlayerBaseState Auto(){return new PlayerAutoState(_context, this);}

}
