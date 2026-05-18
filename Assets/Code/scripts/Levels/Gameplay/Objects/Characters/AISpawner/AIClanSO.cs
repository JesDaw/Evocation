using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scriptable Object containing all AI behavior for a clan
/// Can be swapped per level to change AI difficulty/behavior
/// </summary>
[CreateAssetMenu(fileName = "New AI Clan", menuName = "AI/Clan Configuration")]
public class AIClanSO : ScriptableObject
{
    [Header("Clan Info")]
    public string clanName = "New Clan";
    [TextArea(3, 5)]
    public string description = "Description of this AI's behavior";
    
    [Header("AI Moods")]
    [Tooltip("Different behavior patterns the AI can switch between")]
    public List<AIPersonality> moods = new List<AIPersonality>();
    
    [Tooltip("Which mood to start with (index)")]
    public int startingMoodIndex = 0;
}

[System.Serializable]
public class AIPersonality
{
    [Header("Mood Info")]
    public string moodName = "Mood";
    
    [TextArea(3, 5)]
    public string description = "Description of this mood's behavior";
    
    [Header("Decision Loops")]
    [Tooltip("Each loop operates independently with its own timing")]
    public List<AILoop> decisionLoops = new List<AILoop>();
}

[System.Serializable]
public class AILoop
{
    [Header("Loop Info")]
    [Tooltip("Disable to skip this loop entirely")]
    [SerializeField] public bool enabled = true;
    public string loopName = "New Loop";
    
    [Header("Timing")]
    [Tooltip("Base interval in seconds between decisions")]
    public float baseInterval = 3f;
    
    [Tooltip("Random variance (+/- seconds) to add unpredictability")]
    public float intervalVariance = 0.5f;
    
    [Header("Available Actions")]
    [Tooltip("All possible actions this loop can choose from")]
    [SerializeReference, SubclassSelector]
    public List<AIAction> possibleActions = new List<AIAction>();
    
    [Header("Debug")]
    [Tooltip("Toggle debug logs for this specific loop")]
    [SerializeField] public bool showDebugLogs = false;
    
    // Runtime state (not serialized)
    [System.NonSerialized] public float currentTimer = 0f;
    [System.NonSerialized] public float currentInterval = 0f;
    [System.NonSerialized] public bool isExecutingSequence = false;
    
    /// <summary>
    /// Initialize the loop for first use
    /// </summary>
    public void Initialize()
    {
        ResetTimer();
    }
    
    /// <summary>
    /// Reset timer with a new random interval
    /// </summary>
    public void ResetTimer()
    {
        currentInterval = baseInterval + Random.Range(-intervalVariance, intervalVariance);
        currentTimer = 0f;
    }
    
    /// <summary>
    /// Add extra time to the current interval (for pauses, delays, etc)
    /// </summary>
    public void AddDelay(float additionalTime)
    {
        currentInterval += additionalTime;
    }
    
    /// <summary>
    /// Update the timer and check if it's time to make a decision
    /// </summary>
    public bool UpdateTimer(float deltaTime)
    {
        if (isExecutingSequence)
            return false;
            
        currentTimer += deltaTime;
        return currentTimer >= currentInterval;
    }
}