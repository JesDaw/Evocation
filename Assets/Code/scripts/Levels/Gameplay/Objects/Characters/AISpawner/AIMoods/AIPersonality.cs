using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI Mood/Personality - defines available actions and modifiers
/// Now uses SubclassSelector for clean inspector experience
/// </summary>
[System.Serializable]
public class AIMood
{
    [Header("Mood Info")]
    public string moodName = "Mood";

    [TextArea(3, 5)]
    public string description = "Notes for if we need to remember what this mood is suposed to be like";

    [Header("Available Actions")]
    [SerializeReference, SubclassSelector]
    public List<AIAction> availableActions = new List<AIAction>();
}