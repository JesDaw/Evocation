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
    public string moodName = "Balanced";
    
    [TextArea(3, 5)]
    public string description = "Describe this AI mood/strategy";
    
    [Header("Utility Modifiers")]
    [Tooltip("Multiply ALL action utilities by this")]
    [Range(0.1f, 3f)]
    public float globalUtilityMultiplier = 1f;
    
    [Tooltip("Add this flat bonus to all action utilities")]
    public float globalUtilityBonus = 0f;
    
    [Header("Available Actions")]
    [Tooltip("What can the AI do in this mood?")]
    public List<AIActionWrapper> availableActions = new List<AIActionWrapper>();

    /// <summary>
    /// Apply this mood's modifiers to an action's utility
    /// </summary>
    public float ModifyUtility(float baseUtility, AIActionWrapper actionWrapper)
    {
        float modified = baseUtility;
        
        // Apply global modifiers
        modified = (modified + globalUtilityBonus) * globalUtilityMultiplier;
        
        // Apply action-specific modifiers
        modified = (modified + actionWrapper.bonusUtility) * actionWrapper.utilityMultiplier;
        
        return modified;
    }
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
    [Tooltip("Only used if actionType = SpawnUnit")]
    public SpawnUnitAction spawnAction = new SpawnUnitAction();
    
    [Header("Wait Action Settings")]
    [Tooltip("Only used if actionType = DoNothing")]
    public DoNothingAction doNothingAction = new DoNothingAction();
    
    [Header("Mood-Specific Modifiers")]
    [Tooltip("Multiply this action's utility in this mood")]
    public float utilityMultiplier = 1f;
    
    [Tooltip("Add flat bonus to this action in this mood")]
    public float bonusUtility = 0f;

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

/// <summary>
/// Types of actions available
/// Add more action types here as you create them
/// </summary>
public enum ActionType
{
    SpawnUnit,
    DoNothing
    // Add more types here:
    // UpgradeMoney,
    // SendAllUnits,
    // etc.
}