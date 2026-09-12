using UnityEngine;

public class TestingSpawner : MonoBehaviour
{
    [SerializeField] SpawnObjects alliedSpawnObject;
    [SerializeField] TestSpawn[] AllyUnitsToSpawn;
    [SerializeField] SpawnObjects enemySpawnObject;
    [SerializeField] TestSpawn[] EnemyUnitsToSpawn;

    public void SpawnEnemy()
    {
        foreach (var Character in EnemyUnitsToSpawn)
        {
            for (int i = 0; i < Character.number; i++)
            {
                enemySpawnObject.SpawnCPU(Character.character);
            }
        }
    }

    public void SpawnAlly()
    {
        foreach (var Character in AllyUnitsToSpawn)
        {
            for (int i = 0; i < Character.number; i++)
            {
                alliedSpawnObject.SpawnCPU(Character.character);
            }
        }
    }
}

[System.Serializable]
public class TestSpawn
{
    public int number = 1;
    public ScriptableStats character;
}
