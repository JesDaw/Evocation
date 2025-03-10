using UnityEngine;

public class CpuUtilis: MonoBehaviour
{
    //this script should be on every cpu
    public void SelectOnAttack(int n, ScriptableStats CreatedStats)
    {
        switch(n)
        {
            case 1:
                SpawnMob(CreatedStats);
            case default:
                Debug.LogWarning("Invalid On Attack");
        }
    }
    public void SpawnMob(ScriptableStats ScrStats)
    {
        Debug.Log("Spawned: " + ScrStats.name);
    }
}
