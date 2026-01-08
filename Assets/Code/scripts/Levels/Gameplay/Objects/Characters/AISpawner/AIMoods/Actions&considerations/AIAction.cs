using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for all AI actions
/// Now uses flexible consideration system!
/// </summary>
public abstract class AIAction : ScriptableObject
{
    [Header("Action Info")]
    public string actionName = "AI Action";
    
    [TextArea(2, 4)]
    public string description = "What does this action do?";
    
    [Header("Considerations")]
    [Tooltip("All factors this action considers - completely flexible!")]
    public List<AIConsideration> considerations = new List<AIConsideration>();
    
    [Header("Bonus Utility")]
    [Tooltip("Flat bonus added to utility (use sparingly)")]
    public float flatBonus = 0f;

    /// <summary>
    /// Calculate utility by evaluating all considerations
    /// </summary>
    public virtual float CalculateUtility(AIContext context)
    {
        if (!CanExecute(context))
            return float.MinValue; // Can't execute = terrible utility
        
        float totalUtility = flatBonus;
        
        // Sum up all considerations
        foreach (AIConsideration consideration in considerations)
        {
            if (consideration != null)
            {
                float considerationValue = consideration.Evaluate(context);
                totalUtility += considerationValue;
            }
        }
        
        return totalUtility;
    }

    /// <summary>
    /// Execute this action - implement in derived classes
    /// </summary>
    public abstract void Execute(AIContext context);

    /// <summary>
    /// Can this action be executed right now?
    /// Override in derived classes for specific requirements
    /// </summary>
    public virtual bool CanExecute(AIContext context)
    {
        return true;
    }
}