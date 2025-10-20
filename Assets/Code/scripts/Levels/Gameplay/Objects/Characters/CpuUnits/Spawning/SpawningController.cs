using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class SpawningController : MonoBehaviour
{
    public SpawnObjects spawnObjects;
    public float SpawnCondition;
    private BaseActionInfo _info;
    public List<BaseActionInfo> SpawningInfo;
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

        spawnObjects.SpawnFromSpawner(_stat.stat);
    }

    IEnumerator SpawnLoop()
    {
        foreach (BaseActionInfo _stat in SpawningInfo)
        {
            if (_stat.ConditionLower > SpawnCondition) continue;
            if (_stat.ConditionUpper < SpawnCondition) continue;
            _info = _stat;
            switch (_stat)
            {
                case SpawningStatsInfo _spawn:
                    Spawn(_spawn);
                    break;
                case DebugStatsInfo _Debug:
                    Debug.Log(_Debug.debugText);
                    break;
                default:
                    break;
            }
            continue;
        }

        if(_info != null)
        {
            yield return new WaitForSeconds(_info.conditionRate);
        }
        else
        {
            yield return new WaitForSeconds(3);
        }
    }

    private IEnumerator SpawnLoopRepeater()
    {
        while (true)
        {
            yield return StartCoroutine(SpawnLoop());
        }
    }
}


