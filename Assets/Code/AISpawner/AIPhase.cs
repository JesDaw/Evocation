using UnityEngine;
/// <summary>
/// This is the overll strategy of the AI
/// basically if you want the entire playstyle to change then change these 
/// like phase 2 or boss phases or whatever 
/// Phases themselves should not decide when to switch or what phase to switch to, 
/// that should be decided by the external game manager
/// </summary>

public abstract class AIPhase
{
    AISubState[] _subStates;
    public AISubState[] GetSubStates(ScriptableStats[] spawnableCharacters)
    {
        if (_subStates == null) _subStates = BuildSubStates(spawnableCharacters);
        return _subStates;
    }

    protected abstract AISubState[] BuildSubStates(ScriptableStats[] spawnableCharacters);

    public AISubState EvaluateSubState(AILevelHarness harness, ScriptableStats[] spawnableCharacters)
    {
        foreach (var subState in GetSubStates(spawnableCharacters))
        {
            if (subState.CanEnter(harness))
            {
                return subState;
            }
        }
        return null;
    }
}