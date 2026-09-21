using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TestingSpawner))]
public class TestingSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TestingSpawner testingSpawner = (TestingSpawner)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Spawn Enemy CPU"))
        {
            testingSpawner.SpawnEnemy();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Spawn Ally CPU"))
        {
            testingSpawner.SpawnAlly();
        }
    }
}
