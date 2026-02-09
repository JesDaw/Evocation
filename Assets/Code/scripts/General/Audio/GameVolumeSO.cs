using UnityEngine;

[CreateAssetMenu(fileName = "GameVolumeSO", menuName = "GameVolume")]
public class GameVolumeSO : ScriptableObject
{
    [Header("Volume")]
    [Range(0,1)]
    public float MasterVolume = .5f;
    [Range(0,1)]
    public float MusicVolume = .5f;
    [Range(0,1)]
    public float SFXVolume = .5f;
}
