using UnityEngine;

[System.Serializable]
public class Dialogue
{
    public string CharacterName;
    [TextArea(3, 10)]
    public string Line;
   [Range(.0001f, 1f)]
    public float DialogueDelaySeconds = .2f;
    public int CharacterBody;
    public int CharacterFace;
}
