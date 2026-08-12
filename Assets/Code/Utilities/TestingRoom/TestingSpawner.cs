using UnityEngine;

public class TestingSpawner : MonoBehaviour
{
    [SerializeField] SpawnObjects alliedSpawnObject;
    [SerializeField] SpawnObjects enemySpawnObject;

    // Update is called once per frame
    public void SpawnEnemy(ScriptableStats _spawnEnemy)
    {
        enemySpawnObject.SpawnCPU(_spawnEnemy);
    }

    public void SpawnAlly(ScriptableStats _spawnAlly)
    {
        alliedSpawnObject.SpawnCPU(_spawnAlly);
    }
}
