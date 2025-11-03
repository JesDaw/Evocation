using UnityEngine;

[System.Serializable]
public abstract class CpuAction : ScriptableObject
{
    public CpuSpawnController _context;
    public void AssignController(CpuSpawnController context)
    {
        _context = context;
    }
    public abstract bool EvalBasedOnCondition();
    public abstract void UseAction();
}
