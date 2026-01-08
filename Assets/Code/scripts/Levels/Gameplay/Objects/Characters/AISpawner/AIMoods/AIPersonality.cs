using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI Mood/Personality - defines what actions are available and how they're modified
/// Moods are now changed manually via events, not auto-evaluated!
/// Think of these as "game phases" or "AI strategies"
/// </summary>
[CreateAssetMenu(fileName = "NewMood", menuName = "AI/Mood")]
public class AIPersonality : ScriptableObject
{
    [Header("Mood Info")]
    public string moodName = "Balanced";
    
    [TextArea(3, 5)]
    public string description = "Describe this AI mood/strategy";
    
    [Header("Available Actions")]
    [Tooltip("What can the AI do in this mood?")]
    public List<AIAction> availableActions = new List<AIAction>();
    
    [Header("Utility Modifiers")]
    [Tooltip("Multiply ALL action utilities by this")]
    [Range(0.1f, 3f)]
    public float globalUtilityMultiplier = 1f;
    
    [Tooltip("Add this flat bonus to all action utilities")]
    public float globalUtilityBonus = 0f;
    
    [Header("Action-Specific Modifiers (Optional)")]
    [Tooltip("Special multipliers for specific actions")]
    public List<ActionModifier> actionModifiers = new List<ActionModifier>();

    /// <summary>
    /// Apply this mood's modifiers to an action's utility
    /// </summary>
    public float ModifyUtility(float baseUtility, AIAction action)
    {
        float modified = baseUtility;
        
        // Apply global modifiers
        modified = (modified + globalUtilityBonus) * globalUtilityMultiplier;
        
        // Apply action-specific modifiers
        foreach (ActionModifier modifier in actionModifiers)
        {
            if (modifier.targetAction == action)
            {
                modified = (modified + modifier.bonusUtility) * modifier.utilityMultiplier;
            }
        }
        
        return modified;
    }

    /// <summary>
    /// Check if a specific action is available in this mood
    /// </summary>
    public bool HasAction(AIAction action)
    {
        return availableActions.Contains(action);
    }
}

/// <summary>
/// Special modifier for specific actions within a mood
/// Example: "In Defensive mood, tank spawns get 2x utility"
/// </summary>
[System.Serializable]
public class ActionModifier
{
    public AIAction targetAction;
    public float utilityMultiplier = 1f;
    public float bonusUtility = 0f;
    
    [TextArea(2, 3)]
    public string note = "Why this modifier?";
}