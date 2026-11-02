using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpawnCharacterSingle))]
public class SpawnCharacterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpawnCharacterSingle spawner = (SpawnCharacterSingle)target;
        if(GUILayout.Button("Spawn CPU")) spawner.Spawn();
    }
}
