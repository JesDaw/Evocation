using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UltEvents;

/// <summary>
/// Manages all Timeline cutscenes in the level.
/// Provides named access to cutscenes and handles playback state.
/// Automatically transitions to next state when cutscenes complete (if configured).
/// </summary>
public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance { get; private set; }
    
    [Header("Timeline Configuration")]
    [SerializeField] private List<TimelineEntry> timelines = new List<TimelineEntry>();
    
    [Header("Allow Auto-Transition")]
    [SerializeField] private bool autoTransitionAfterCutscene = true;
    
    [Header("Events")]
    [SerializeField] private UltEvent onCutsceneStart;
    [SerializeField] private UltEvent onCutsceneEnd;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private Dictionary<string, TimelineEntry> timelinesByName = new Dictionary<string, TimelineEntry>();
    private PlayableDirector currentTimeline;
    private string currentTimelineName = "";
    
    [System.Serializable]
    public class TimelineEntry
    {
        public string timelineName;
        public PlayableDirector director;
        [Tooltip("If true, automatically transitions to next level state when this cutscene ends")]
        public bool transitionToNextState = true;
    }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        InitializeTimelines();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from all timeline events
        foreach (var entry in timelines)
        {
            if (entry.director != null)
            {
                entry.director.stopped -= OnTimelineStopped;
            }
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void InitializeTimelines()
    {
        // Set all directors to use unscaled time (for cutscenes during pause)
        foreach (var entry in timelines)
        {
            if (entry.director != null)
            {
                entry.director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                timelinesByName[entry.timelineName] = entry;
                
                // Subscribe to timeline completion
                entry.director.stopped += OnTimelineStopped;
            }
            else
            {
                Debug.LogWarning($"[TimelineManager] Timeline entry '{entry.timelineName}' has no director assigned!");
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[TimelineManager] Initialized {timelines.Count} timelines");
        }
    }
    
    /// <summary>
    /// Play a cutscene by name
    /// </summary>
    public void PlayCutscene(string timelineName)
    {
        if (timelinesByName.TryGetValue(timelineName, out TimelineEntry entry))
        {
            if (entry.director.state != PlayState.Playing)
            {
                currentTimeline = entry.director;
                currentTimelineName = timelineName;
                
                // Configure for cutscene
                if (GlobalInputManager.Instance != null)
                {
                    GlobalInputManager.Instance.SetCutsceneMode();
                }
                
                // Notify listeners
                onCutsceneStart?.Invoke();
                
                // Play the timeline
                //WaitBeforeStarting(entry);
                StartCoroutine(WaitBeforeStarting(entry));
                //entry.director.Play();
                
                if (showDebugLogs)
                    Debug.Log($"[TimelineManager] Playing cutscene: {timelineName}");
            }
            else
            {
                Debug.LogWarning($"[TimelineManager] Cutscene '{timelineName}' is already playing!");
            }
        }
        else
        {
            Debug.LogError($"[TimelineManager] Cutscene '{timelineName}' not found! Available: {string.Join(", ", timelinesByName.Keys)}");
        }
    }
    System.Collections.IEnumerator WaitBeforeStarting(TimelineEntry entry)
    {
        //Debug.Log("WaitBeforeStarting start");
        yield return null; 
        entry.director.Play();
        //Debug.Log("WaitBeforeStarting end");
    }
    
    /// <summary>
    /// Play a cutscene by index (backward compatibility)
    /// </summary>
    public void PlayCutscene(int index)
    {
        if (index >= 0 && index < timelines.Count)
        {
            PlayCutscene(timelines[index].timelineName);
        }
        else
        {
            Debug.LogError($"[TimelineManager] Invalid cutscene index: {index}. Valid range: 0-{timelines.Count - 1}");
        }
    }
    
    /// <summary>
    /// Stop the currently playing cutscene
    /// </summary>
    public void StopCurrentCutscene()
    {
        if (currentTimeline != null && currentTimeline.state == PlayState.Playing)
        {
            currentTimeline.Stop();
            currentTimeline = null;
            currentTimelineName = "";
            
            if (showDebugLogs)
                Debug.Log("[TimelineManager] Current cutscene stopped");
        }
    }
    
    /// <summary>
    /// Skip to the end of the current cutscene
    /// </summary>
    public void SkipCurrentCutscene()
    {
        if (currentTimeline != null && currentTimeline.state == PlayState.Playing)
        {
            currentTimeline.time = currentTimeline.duration;
            currentTimeline.Evaluate();
            
            if (showDebugLogs)
                Debug.Log($"[TimelineManager] Skipped cutscene: {currentTimelineName}");
        }
    }
    
    /// <summary>
    /// Pause/resume the current cutscene
    /// </summary>
    public void PauseCurrentCutscene(bool pause)
    {
        if (currentTimeline != null)
        {
            if (pause)
                currentTimeline.Pause();
            else
                currentTimeline.Resume();
            
            if (showDebugLogs)
                Debug.Log($"[TimelineManager] Cutscene {(pause ? "paused" : "resumed")}");
        }
    }
    
    /// <summary>
    /// Check if any cutscene is currently playing
    /// </summary>
    public bool IsCutscenePlaying()
    {
        return currentTimeline != null && currentTimeline.state == PlayState.Playing;
    }
    
    /// <summary>
    /// Get the name of the currently playing cutscene
    /// </summary>
    public string GetCurrentCutsceneName()
    {
        return currentTimelineName;
    }
    
    /// <summary>
    /// Get a timeline by name
    /// </summary>
    public PlayableDirector GetTimeline(string timelineName)
    {
        if (timelinesByName.TryGetValue(timelineName, out TimelineEntry entry))
        {
            return entry.director;
        }
        return null;
    }
    
    /// <summary>
    /// Get all timeline names
    /// </summary>
    public List<string> GetAllTimelineNames()
    {
        return new List<string>(timelinesByName.Keys);
    }
    
    /// <summary>
    /// Check if a timeline exists
    /// </summary>
    public bool HasTimeline(string timelineName)
    {
        return timelinesByName.ContainsKey(timelineName);
    }
    
    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director == currentTimeline)
        {
            string timelineName = GetTimelineName(director);
            
            if (showDebugLogs)
                Debug.Log($"[TimelineManager] Cutscene completed: {timelineName}");
            
            // Notify listeners
            onCutsceneEnd?.Invoke();
            
            // Auto-transition if enabled
            if (autoTransitionAfterCutscene && 
                timelinesByName.TryGetValue(timelineName, out TimelineEntry entry) && 
                entry.transitionToNextState)
            {
                if (LevelStateManager.Instance != null)
                {
                    if (showDebugLogs)
                        Debug.Log($"[TimelineManager] Auto-transitioning to next state after cutscene: {timelineName}");
                    
                    LevelStateManager.Instance.TransitionToNextState();
                }
            }
            
            currentTimeline = null;
            currentTimelineName = "";
        }
    }
    
    private string GetTimelineName(PlayableDirector director)
    {
        foreach (var entry in timelinesByName.Values)
        {
            if (entry.director == director)
                return entry.timelineName;
        }
        return "Unknown";
    }
}