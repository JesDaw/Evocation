using UnityEngine;

[CreateAssetMenu]
[System.Serializable]
public class DialogueSO : ScriptableObject
{
    public string CharacterName;

    [TextArea(3, 10)]
    public string[] Lines;
    public string[] DialogueSpeedOveride;
    public string[] CharacterNameOveride;

}
