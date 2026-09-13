using UnityEngine;

public abstract class AIClan : ScriptableObject
{
    public ScriptableStats[] SpawnableCharacters;
    private AIPhase[] _phases;

    public AIPhase[] Phases
    {
        get
        {
            if (_phases == null) _phases = BuildPhases();
            return _phases;
        }
    }

    protected abstract AIPhase[] BuildPhases();
}