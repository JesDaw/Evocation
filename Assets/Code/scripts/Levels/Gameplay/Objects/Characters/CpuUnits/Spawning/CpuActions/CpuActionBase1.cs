using UnityEngine;

[System.Serializable]

[CreateAssetMenu(fileName = "Spawning Stats", menuName = "Cpu Actions/Spawning Stats")]
public class SpawningStatsInfo : BaseActionInfo 
{
    public ScriptableStats stat;
}
