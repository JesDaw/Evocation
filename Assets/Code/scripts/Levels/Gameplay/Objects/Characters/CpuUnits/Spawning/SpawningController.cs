using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public abstract class SpawningController : MonoBehaviour
{
    public SpawnObjects spawnObjects;
    public List<SpawningStatsInfo> SpawningInfo;
    public abstract IEnumerator SpawnLoop();
    private void Start()
    {
        StartCoroutine(SpawnLoopRepeater());
    }
    public void Spawn(int stat)
    {
        if (!SpawningInfo[stat].spawnable) return;
        spawnObjects.SpawnFromSpawner(SpawningInfo[stat].stat);
    }

    private IEnumerator SpawnLoopRepeater()
    {
        while (true)
        {
            yield return StartCoroutine(SpawnLoop());
        }
    }
}

[System.Serializable]
public class SpawningStatsInfo
{
    public ScriptableStats stat;
    public bool spawnable;
}
