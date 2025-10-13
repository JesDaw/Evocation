using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class SpawningController : MonoBehaviour
{
    public SpawnObjects spawnObjects;
    public float SpawnCondition;
    private SpawningStatsInfo _info;
    public List<SpawningStatsInfo> SpawningInfo;
    void OnValidate()
    {
        SpawnCondition = Mathf.Clamp(SpawnCondition, 0f, 1f);
    }
    private void Start()
    {
        StartCoroutine(SpawnLoopRepeater());
    }
    public void Spawn(SpawningStatsInfo _stat)
    {
        if (_stat.ConditionLower > SpawnCondition) return;
        if (_stat.ConditionUpper < SpawnCondition) return;
        _info = _stat;
        spawnObjects.SpawnFromSpawner(_stat.stat);
    }

    IEnumerator SpawnLoop()
    {
        foreach (SpawningStatsInfo _stat in SpawningInfo)
        {
            Spawn(_stat);
            continue;
        }

        yield return new WaitForSeconds(_info.spawnRate);
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
    public float ConditionLower;
    public float ConditionUpper;
    public float spawnRate;
}
