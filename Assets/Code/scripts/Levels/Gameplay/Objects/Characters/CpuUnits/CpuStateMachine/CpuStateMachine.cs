using UnityEngine;

public class CpuStateMachine : MonoBehaviour
{
    public CpuStateFactory cpuStateFactory;
    public CpuBaseState cpuBaseState;
    void Start()
    {
        cpuBaseState.EnterState();    
    }
}
