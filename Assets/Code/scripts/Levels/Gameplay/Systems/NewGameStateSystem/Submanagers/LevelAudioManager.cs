using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

public class LevelAudioManager : MonoBehaviour
{
    public static LevelAudioManager Instance { get; private set; }
    
    [Header("FMOD Events Source")]
    [SerializeField] FModEvents fmodEvents;
    
    [Header("Music Configuration")]
    [SerializeField] string musicParameterName = "MountainLevelPhases";
    
    [Header("Music State Mapping")]
    [SerializeField] List<MusicStateMapping> musicStates = new List<MusicStateMapping>();
    [SerializeField] bool StartMusicOnAwake = false;
    
    [Header("Debug")]
    [SerializeField] bool showDebugLogs = false;
    
    EventInstance musicInstance;
    EventInstance ambienceInstance;
    Dictionary<string, float> stateValues = new Dictionary<string, float>();
    bool musicIsPlaying = false;
    string currentMusicState = "";
    
    [System.Serializable]
    public class MusicStateMapping
    {
        public string stateName;
        public float parameterValue;
    }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (fmodEvents == null)
        {
            fmodEvents = FModEvents.instance;
        }
        
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
        
        if (!fmodEvents.music.IsNull)
        {
            musicInstance = RuntimeManager.CreateInstance(fmodEvents.music);
            if(StartMusicOnAwake) StartMusic();
            if (showDebugLogs) Debug.Log("[LevelAudioManager] Music system initialized");
        }
        else
        {
            Debug.LogWarning("[LevelAudioManager] Music event reference not set in FModEvents!");
        }
        
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
    
    public void StopMusic(FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
        if (!musicInstance.isValid()) return;
        
        musicInstance.stop(stopMode);
        musicIsPlaying = false;
        
        if (showDebugLogs)
            Debug.Log($"[LevelAudioManager] Music stopped ({stopMode})");
    }
    public void PauseMusic(bool pause)
    {
        if (!musicInstance.isValid()) return;
        
        musicInstance.setPaused(pause);
        
        if (showDebugLogs)
            Debug.Log($"[LevelAudioManager] Music {(pause ? "paused" : "resumed")}");
    }
    
    public string GetCurrentMusicState()
    {
        return currentMusicState;
    }
    
    public bool IsMusicPlaying()
    {
        return musicIsPlaying;
    }
    
    #endregion
    
    #region Ambience Control

    public void StartAmbience()
    {
        if (ambienceInstance.isValid())
        {
            ambienceInstance.start();
            if (showDebugLogs) Debug.Log("[LevelAudioManager] Ambience started");
        }
    }
    
    public void StopAmbience(FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
        if (ambienceInstance.isValid())
        {
            ambienceInstance.stop(stopMode);
            if (showDebugLogs) Debug.Log($"[LevelAudioManager] Ambience stopped ({stopMode})");
        }
    }
    
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
    
    public void PlayOneShot(EventReference sound, Vector3 worldPosition)
    {
        RuntimeManager.PlayOneShot(sound, worldPosition);
    }
    
    public void PlayOneShot(EventReference sound)
    {
        RuntimeManager.PlayOneShot(sound);
    }
    
    public void PlayMenuClick()
    {
        if (fmodEvents != null && !fmodEvents.menuClick.IsNull)
        {
            PlayOneShot(fmodEvents.menuClick);
        }
    }
    
    #endregion
    
    #region Utility
    
    public List<string> GetAvailableMusicStates()
    {
        return new List<string>(stateValues.Keys);
    }

    public bool HasMusicState(string stateName)
    {
        return stateValues.ContainsKey(stateName);
    }
    
    #endregion
}