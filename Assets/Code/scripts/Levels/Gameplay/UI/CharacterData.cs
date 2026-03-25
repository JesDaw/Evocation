using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Character Select/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    [TextArea] public string description;
    public Sprite portrait;
    public Sprite headshot;
    public ScriptableStats scriptableStats;
    public string SoundName = "";
}