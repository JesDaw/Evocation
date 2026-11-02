using UnityEngine;
using UnityEditor;
using System.Collections;

[RequireComponent(typeof(SpawnObjects))]
public class SpawnCharacterSingle : MonoBehaviour
{
    SpawnObjects spawnObject;
    [SerializeField] ScriptableStats spawnStats;

    void Awake()
    {
        spawnObject = GetComponent<SpawnObjects>();
    }

    public void Spawn()
    {
        spawnObject.SpawnCPU(spawnStats);
    }
}
