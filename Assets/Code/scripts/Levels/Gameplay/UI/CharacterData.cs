using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Character Select/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public int RelationshipClanID;
    public int RelationshipLevelRequironment = 0;
    [TextArea] public string description;
    public Sprite portrait;
    public Sprite headshot;
    public ScriptableStats scriptableStats;
    public string SoundName = "";
}