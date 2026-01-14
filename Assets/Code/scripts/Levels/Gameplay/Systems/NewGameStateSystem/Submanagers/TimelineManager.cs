using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UltEvents;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance { get; private set; }
    
    [Header("Timeline Configuration")]
    [SerializeField] List<TimelineEntry> timelines = new List<TimelineEntry>();
    
    [Header("Allow Auto-Transition")]
    [SerializeField] bool autoTransitionAfterCutscene = true;
    
    [Header("Events")]
    [SerializeField] UltEvent onCutsceneStart;
    [SerializeField] UltEvent onCutsceneEnd;
    
    [Header("Debug")]
    [SerializeField] bool showDebugLogs = false;
    
    Dictionary<string, TimelineEntry> timelinesByName = new Dictionary<string, TimelineEntry>();
    PlayableDirector currentTimeline;
    TimelineController currentTimelineController;
    string currentTimelineName = "";
    
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
        foreach (var entry in timelines)
        {
            if (entry.director != null)
            {
                entry.director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                timelinesByName[entry.timelineName] = entry;
                
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
    
    public void PlayCutscene(string timelineName)
    {
        if (timelinesByName.TryGetValue(timelineName, out TimelineEntry entry))
        {
            if (entry.director.state != PlayState.Playing)
            {
                currentTimeline = entry.director;
                currentTimelineName = timelineName;
                currentTimelineController = currentTimeline.gameObject.GetComponent<TimelineController>();
                
                if (GlobalInputManager.Instance != null)
                {
                    GlobalInputManager.Instance.SetCutsceneMode();
                }
                
                onCutsceneStart?.Invoke();
                
                StartCoroutine(WaitBeforeStarting(entry));
                
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
        yield return null; 
        entry.director.Play();
    }
    
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

    public void StopCurrentCutscene()
    {
        if (currentTimelineController != null && currentTimeline.state == PlayState.Playing)
        {
            currentTimelineController.ResetTimeline();
            currentTimeline = null;
            currentTimelineController = null;
            currentTimelineName = "";
            
            if (showDebugLogs)
                Debug.Log("[TimelineManager] Current cutscene stopped");
        }
    }

    public void SkipCurrentCutscene()
    {
        if (currentTimelineController != null && currentTimeline.state == PlayState.Playing)
        {
           currentTimelineController.SkipTimeline();
            
            if (showDebugLogs)
                Debug.Log($"[TimelineManager] Skipped cutscene: {currentTimelineName}");
        }
    }
    
    public void PauseCurrentCutscene(bool pause)
    {
        if (currentTimelineController != null)
        {
            if (pause)
                currentTimelineController.PauseGame();
            else
                currentTimelineController.UnpauseGame();
            
            if (showDebugLogs)
                Debug.Log($"[TimelineManager] Cutscene {(pause ? "paused" : "resumed")}");
        }
    }

    public bool IsCutscenePlaying()
    {
        return currentTimeline != null && currentTimeline.state == PlayState.Playing;
    }

    public string GetCurrentCutsceneName()
    {
        return currentTimelineName;
    }

    public PlayableDirector GetTimeline(string timelineName)
    {
        if (timelinesByName.TryGetValue(timelineName, out TimelineEntry entry))
        {
            return entry.director;
        }
        return null;
    }

    public List<string> GetAllTimelineNames()
    {
        return new List<string>(timelinesByName.Keys);
    }
  
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
            
            onCutsceneEnd?.Invoke();
            
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