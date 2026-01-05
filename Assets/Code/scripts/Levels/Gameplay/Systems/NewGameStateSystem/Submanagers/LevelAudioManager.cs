using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

/// <summary>
/// Manages level-specific audio including music states and ambience.
/// Wraps FMOD functionality with a state-based interface.
/// Integrates with FModEvents to access event references.
/// </summary>
public class LevelAudioManager : MonoBehaviour
{
    public static LevelAudioManager Instance { get; private set; }
    
    [Header("FMOD Events Source")]
    [SerializeField] private FModEvents fmodEvents;
    
    [Header("Music Configuration")]
    [SerializeField] private string musicParameterName = "MountainLevelPhases";
    
    [Header("Music State Mapping")]
    [SerializeField] private List<MusicStateMapping> musicStates = new List<MusicStateMapping>();
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private EventInstance musicInstance;
    private EventInstance ambienceInstance;
    private Dictionary<string, float> stateValues = new Dictionary<string, float>();
    private bool musicIsPlaying = false;
    private string currentMusicState = "";
    
    [System.Serializable]
    public class MusicStateMapping
    {
        public string stateName;
        public float parameterValue;
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
        
        // Find FModEvents if not assigned
        if (fmodEvents == null)
        {
            fmodEvents = FModEvents.instance;
        }
        
        // Build state lookup dictionary
        BuildStateLookup();
    }
    
    void Start()
    {
        InitializeAudio();
    }
    
    void OnDestroy()
    {
        CleanupAudio();
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void BuildStateLookup()
    {
        stateValues.Clear();
        foreach (var mapping in musicStates)
        {
            stateValues[mapping.stateName] = mapping.parameterValue;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[LevelAudioManager] Registered {musicStates.Count} music states");
        }
    }
    
    private void InitializeAudio()
    {
        if (fmodEvents == null)
        {
            Debug.LogError("[LevelAudioManager] FModEvents not found! Make sure FModEvents GameObject exists in the scene.");
            return;
        }
        
        // Initialize music
        if (!fmodEvents.music.IsNull)
        {
            musicInstance = RuntimeManager.CreateInstance(fmodEvents.music);
            if (showDebugLogs) Debug.Log("[LevelAudioManager] Music system initialized");
        }
        else
        {
            Debug.LogWarning("[LevelAudioManager] Music event reference not set in FModEvents!");
        }
        
        // Initialize and start ambience
        if (!fmodEvents.ambiance.IsNull)
        {
            ambienceInstance = RuntimeManager.CreateInstance(fmodEvents.ambiance);
            ambienceInstance.start();
            if (showDebugLogs) Debug.Log("[LevelAudioManager] Ambience started");
        }
        else
        {
            Debug.LogWarning("[LevelAudioManager] Ambience event reference not set in FModEvents!");
        }
    }
    
    private void CleanupAudio()
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicInstance.release();
        }
        
        if (ambienceInstance.isValid())
        {
            ambienceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            ambienceInstance.release();
        }
    }
    
    #region Music Control
    
    /// <summary>
    /// Set the music state by name (uses configured state mappings)
    /// </summary>
    public void SetMusicState(string stateName)
    {
        if (!musicInstance.isValid())
        {
            Debug.LogWarning("[LevelAudioManager] Music instance is not valid!");
            return;
        }
        
        if (stateValues.TryGetValue(stateName, out float value))
        {
            currentMusicState = stateName;
            musicInstance.setParameterByName(musicParameterName, value);
            
            if (showDebugLogs)
                Debug.Log($"[LevelAudioManager] Music state set to: {stateName} (value: {value})");
            
            // Start music if not already playing
            if (!musicIsPlaying)
            {
                StartMusic();
            }
        }
        else
        {
            Debug.LogWarning($"[LevelAudioManager] Music state '{stateName}' not found in mappings! Available states: {string.Join(", ", stateValues.Keys)}");
        }
    }
    
    /// <summary>
    /// Set music parameter directly by value
    /// </summary>
    public void SetMusicParameter(float value)
    {
        if (!musicInstance.isValid())
        {
            Debug.LogWarning("[LevelAudioManager] Music instance is not valid!");
            return;
        }
        
        musicInstance.setParameterByName(musicParameterName, value);
        
        if (showDebugLogs)
            Debug.Log($"[LevelAudioManager] Music parameter set to: {value}");
        
        if (!musicIsPlaying)
        {
            StartMusic();
        }
    }
    
    /// <summary>
    /// Start playing music
    /// </summary>
    public void StartMusic()
    {
        if (!musicInstance.isValid())
        {
            Debug.LogWarning("[LevelAudioManager] Music instance is not valid!");
            return;
        }
        
        if (!musicIsPlaying)
        {
            musicInstance.start();
            musicIsPlaying = true;
            
            if (showDebugLogs)
                Debug.Log("[LevelAudioManager] Music started");
        }
    }
    
    /// <summary>
    /// Stop music
    /// </summary>
    public void StopMusic(FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
        if (!musicInstance.isValid()) return;
        
        musicInstance.stop(stopMode);
        musicIsPlaying = false;
        
        if (showDebugLogs)
            Debug.Log($"[LevelAudioManager] Music stopped ({stopMode})");
    }
    
    /// <summary>
    /// Pause music
    /// </summary>
    public void PauseMusic(bool pause)
    {
        if (!musicInstance.isValid()) return;
        
        musicInstance.setPaused(pause);
        
        if (showDebugLogs)
            Debug.Log($"[LevelAudioManager] Music {(pause ? "paused" : "resumed")}");
    }
    
    /// <summary>
    /// Get current music state name
    /// </summary>
    public string GetCurrentMusicState()
    {
        return currentMusicState;
    }
    
    /// <summary>
    /// Check if music is playing
    /// </summary>
    public bool IsMusicPlaying()
    {
        return musicIsPlaying;
    }
    
    #endregion
    
    #region Ambience Control
    
    /// <summary>
    /// Start ambience
    /// </summary>
    public void StartAmbience()
    {
        if (ambienceInstance.isValid())
        {
            ambienceInstance.start();
            if (showDebugLogs) Debug.Log("[LevelAudioManager] Ambience started");
        }
    }
    
    /// <summary>
    /// Stop ambience
    /// </summary>
    public void StopAmbience(FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
        if (ambienceInstance.isValid())
        {
            ambienceInstance.stop(stopMode);
            if (showDebugLogs) Debug.Log($"[LevelAudioManager] Ambience stopped ({stopMode})");
        }
    }
    
    /// <summary>
    /// Pause ambience
    /// </summary>
    public void PauseAmbience(bool pause)
    {
        if (ambienceInstance.isValid())
        {
            ambienceInstance.setPaused(pause);
            if (showDebugLogs) Debug.Log($"[LevelAudioManager] Ambience {(pause ? "paused" : "resumed")}");
        }
    }
    
    #endregion
    
    #region One-Shot Sounds
    
    /// <summary>
    /// Play a one-shot sound effect at a world position
    /// </summary>
    public void PlayOneShot(EventReference sound, Vector3 worldPosition)
    {
        RuntimeManager.PlayOneShot(sound, worldPosition);
    }
    
    /// <summary>
    /// Play a one-shot sound effect at camera position
    /// </summary>
    public void PlayOneShot(EventReference sound)
    {
        RuntimeManager.PlayOneShot(sound);
    }
    
    /// <summary>
    /// Play the menu click sound from FModEvents
    /// </summary>
    public void PlayMenuClick()
    {
        if (fmodEvents != null && !fmodEvents.menuClick.IsNull)
        {
            PlayOneShot(fmodEvents.menuClick);
        }
    }
    
    #endregion
    
    #region Utility
    
    /// <summary>
    /// Get all available music state names
    /// </summary>
    public List<string> GetAvailableMusicStates()
    {
        return new List<string>(stateValues.Keys);
    }
    
    /// <summary>
    /// Check if a music state exists
    /// </summary>
    public bool HasMusicState(string stateName)
    {
        return stateValues.ContainsKey(stateName);
    }
    
    #endregion
}