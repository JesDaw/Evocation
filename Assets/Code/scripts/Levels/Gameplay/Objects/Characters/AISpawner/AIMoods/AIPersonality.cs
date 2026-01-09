using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI Mood/Personality - defines available actions and modifiers
/// Now a serializable class - edit directly in inspector!
/// </summary>
[System.Serializable]
public class AIMood
{
    [Header("Mood Info")]
    public string moodName = "Mood";
    
    [TextArea(3, 5)]
    public string description = "Notes for if we need to remember what this mood is suposed to be like";
    public List<AIActionWrapper> availableActions = new List<AIActionWrapper>();
}

/// <summary>
/// Wrapper for actions with mood-specific modifiers
/// Allows each mood to customize how actions behave
/// </summary>
[System.Serializable]
public class AIActionWrapper
{
    [Header("Action Type")]
    public ActionType actionType;
    
    [Header("Spawn Action Settings")]
    [Tooltip("Only used if actionType is SpawnUnit")]
    public SpawnUnitAction spawnAction = new SpawnUnitAction();
    
    [Header("Wait Action Settings")]
    [Tooltip("Only used if actionType is DoNothing")]
    public DoNothingAction doNothingAction = new DoNothingAction();

    /// <summary>
    /// Get the actual action based on type
    /// </summary>
    public AIAction GetAction()
    {
        switch (actionType)
        {
            case ActionType.SpawnUnit:
                return spawnAction;
            case ActionType.DoNothing:
                return doNothingAction;
            default:
                return null;
        }
    }
}

public enum ActionType
{
    SpawnUnit,
    DoNothing
}