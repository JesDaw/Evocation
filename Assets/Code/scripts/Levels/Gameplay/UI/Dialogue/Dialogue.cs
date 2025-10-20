using UnityEngine;

[CreateAssetMenu]
[System.Serializable]
public class Dialogue
{
    public string CharacterName;
    [TextArea(3, 10)]
    public string Line;
    public float DialogueSpeed;

}
