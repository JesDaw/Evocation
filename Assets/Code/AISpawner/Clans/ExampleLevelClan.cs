using UnityEngine;

[CreateAssetMenu(fileName = "Example Level Clan", menuName = "AIClan/Example Level")]
public class ExampleLevelClan : AIClan
{
    protected override AIPhase[] BuildPhases()
    {
        return new AIPhase[]
        {
            new OpeningPhase(),
            new PressurePhase(),
        };
    }
}

#region Phases
public class OpeningPhase : AIPhase
{
    protected override AISubState[] BuildSubStates(ScriptableStats[] spawnableCharacters) => new AISubState[]
    {
        new TurtleSubState(spawnableCharacters),
    };
}

public class PressurePhase : AIPhase
{
    protected override AISubState[] BuildSubStates(ScriptableStats[] spawnableCharacters) => new AISubState[]
    {
        new RushSubState(spawnableCharacters),
        new TurtleSubState(spawnableCharacters),
    };
}
#endregion

#region Substates
public class TurtleSubState : AISubState 
{
    public TurtleSubState(ScriptableStats[] spawnableCharacters) : base(spawnableCharacters) { }

    public override bool CanEnter(AILevelHarness harness) => true;

    public override void ExecuteFlowchart(AILevelHarness harness)
    {
        if (SpawnableCharacters.Length > 0 && SpawnableCharacters[0] != null)
        {
            if (Money.AIInstance.CurrentMoney >= 50)
            {
                if(harness.showDebugLogs) Debug.Log($" Money.AIInstance.CurrentMoney >= 50: = {Money.AIInstance.CurrentMoney}");
                if (AIAction.CanExecuteSpawnAction(SpawnableCharacters[0])) 
                {
                    if(harness.showDebugLogs) Debug.Log($"AIAction.CanExecuteSpawnAction(SpawnableCharacters[0])");
                    AIAction.ExecuteSpawnAction(SpawnableCharacters[0]);
                }
                else if(harness.showDebugLogs)
                {
                    Debug.Log($"!!AIAction.CanExecuteSpawnAction(SpawnableCharacters[0])");
                }
            }
            else if(harness.showDebugLogs)
            {
                Debug.Log($"!!Money.AIInstance.CurrentMoney >= 50: = {Money.AIInstance.CurrentMoney}");
            }
        }
        else if (harness.showDebugLogs)
        {
            Debug.Log($"!!SpawnableCharacters.Length > 0 && SpawnableCharacters[0] != null");
        }
    }
}

public class RushSubState : AISubState
{
    public RushSubState(ScriptableStats[] spawnableCharacters) : base(spawnableCharacters) { }

    public override bool CanEnter(AILevelHarness harness)
    {
        return false;
    }

    public override void ExecuteFlowchart(AILevelHarness harness)
    {
        if (SpawnableCharacters != null && SpawnableCharacters.Length > 1 && Money.AIInstance != null && Money.AIInstance.CurrentMoney >= 150)
        {
            if (AIAction.CanExecuteSpawnAction(SpawnableCharacters[1]))
            {
                AIAction.ExecuteSpawnAction(SpawnableCharacters[1]);
            }
        }
    }
}
#endregion