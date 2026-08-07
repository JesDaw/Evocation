using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TestingSpawner))]
public class TestingSpawnerEditor : Editor
{
    private ScriptableStats alliedCPU;
    private ScriptableStats enemyCPU;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TestingSpawner testingSpawner = (TestingSpawner)target;

        GUILayout.Space(10);

        enemyCPU = (ScriptableStats)EditorGUILayout.ObjectField(
            "Enemy CPU",
            enemyCPU,
            typeof(ScriptableStats),
            false);

        if (GUILayout.Button("Spawn Enemy CPU"))
        {
            if (enemyCPU != null)
            {
                testingSpawner.SpawnEnemy(enemyCPU);
            }
        }

        GUILayout.Space(10);

        alliedCPU = (ScriptableStats)EditorGUILayout.ObjectField(
            "Ally CPU",
            alliedCPU,
            typeof(ScriptableStats),
            false);

        if (GUILayout.Button("Spawn Ally CPU"))
        {
            if (alliedCPU != null)
            {
                testingSpawner.SpawnAlly(alliedCPU);
            }
        }
    }
}
