/// <summary>
/// substates = operations
/// every substate should have a spacific subgoal that its trying to achieve
/// options for subgoals = trying to get some resource to a certain amount 
/// resources to player around = money, space, time
/// within the substaes are the tactic aka what action are teken
/// I still dont know how to make corrdinated decisions thoigh. Like cordinated pushes by spawning a certain combo of characters or 
/// trying to claim a certain area
/// </summary>
 
public abstract class AISubState
{
    public ScriptableStats[] SpawnableCharacters;

    public AISubState(ScriptableStats[] spawnableCharacters)
    {
        this.SpawnableCharacters = spawnableCharacters;
    }

    public abstract bool CanEnter(AILevelHarness harness);
    public abstract void ExecuteFlowchart(AILevelHarness harness);
}