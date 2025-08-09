using UnityEngine;

public class SoundTrackEffectCaller : MonoBehaviour
{
    [SerializeField] bool ManualTest = false;
    bool _testing = false;
    
    [SerializeField] AudioSource SampleSource;
    [SerializeField] MusicManager musicManager;

    void Start()
    {
        ManualTest = false;
        _testing = false;
        
        // Validate sample source
        if (SampleSource == null)
        {
            Debug.LogError($"SampleSource is not assigned on {gameObject.name}");
        }
    }
    
    void Update()
    {
        if (_testing == ManualTest) return;
        if (ManualTest && !_testing)
        {
            ApplySampleEffects();
            _testing = true;
        }
        else if (!ManualTest && _testing)
        {
            RevertAllEffects();
            _testing = false;
        }
    }

    public void ApplySampleEffects()
    {
        if (SampleSource == null)
        {
            Debug.LogError("Sample source is null - cannot apply effects");
            return;
        }
        
        if (musicManager == null)
        {
            Debug.LogError("Music manager is not connected to this SoundTrackEffectCaller");
            return;
        }
        
        musicManager.ApplyTrackEffects(SampleSource);
        Debug.Log($"Applied sample effects from {SampleSource.name}");
    }

    public void RevertAllEffects()
    {
        if (musicManager == null) 
        {
            Debug.LogError("Music manager isn't connected to this SoundTrackEffectCaller");
        }
        else
        {
            musicManager.ResetAllPlayingTrackEffects();
            Debug.Log("Reverted all track effects");
        }
    }
}