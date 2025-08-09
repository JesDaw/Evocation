using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioEffectEditor
{
    public AudioSource Track;
    
    // Original values storage
    int _originalPriority;
    float _originalVolume;
    float _originalPitch;
    float _originalStereoPan;
    float _originalSpacialBlend;
    float _originalReverbZoneMix;
    float _originalDopplerLevel;
    float _originalSpread;
    float _originalMinDistance;
    float _originalMaxDistance;
    
    // Store volume at time of effect application to restore properly
    float _volumeWhenEffectApplied;
    
    public AudioEffectEditor(AudioSource Track)
    {
        this.Track = Track;
        
        // Store original values when the AudioSource was first created
        _originalPriority = Track.priority;
        _originalVolume = Track.volume;
        _originalPitch = Track.pitch;
        _originalStereoPan = Track.panStereo;
        _originalSpacialBlend = Track.spatialBlend;
        _originalReverbZoneMix = Track.reverbZoneMix;
        _originalDopplerLevel = Track.dopplerLevel;
        _originalSpread = Track.spread;
        _originalMinDistance = Track.minDistance;
        _originalMaxDistance = Track.maxDistance;
        
        Debug.Log($"AudioEffectEditor created for {Track.name} - Original Volume: {_originalVolume}");
    }

    public void ApplyEffects(AudioSource sampleSource)
    {
        if (sampleSource == null)
        {
            Debug.LogError("Sample source is null - cannot apply effects");
            return;
        }
        
        // Store the current volume when effect is applied so we can restore it later
        _volumeWhenEffectApplied = Track.volume;
        
        // Apply all effects from sample (but handle volume specially)
        Track.priority = sampleSource.priority;
        Track.pitch = sampleSource.pitch;
        Track.panStereo = sampleSource.panStereo;
        Track.spatialBlend = sampleSource.spatialBlend;
        Track.reverbZoneMix = sampleSource.reverbZoneMix;
        Track.dopplerLevel = sampleSource.dopplerLevel;
        Track.spread = sampleSource.spread;
        Track.minDistance = sampleSource.minDistance;
        Track.maxDistance = sampleSource.maxDistance;
        
        // For volume: apply the sample's volume but maintain the current fade state
        // Calculate what percentage of max volume we're currently at
        float currentVolumePercentage = Track.volume / 1f; // Assuming max volume is 1
        
        // Apply the sample volume as the new "max volume" but scaled by current percentage
        Track.volume = sampleSource.volume * currentVolumePercentage;
        
        Debug.Log($"Applied effects to {Track.name} from {sampleSource.name} - Volume before: {_volumeWhenEffectApplied}, Volume after: {Track.volume}");
    }

    public void ResetEffects()
    {
        if (Track == null)
        {
            Debug.LogWarning("Track reference is null - cannot reset effects");
            return;
        }
        
        // Reset all properties to original values
        Track.priority = _originalPriority;
        Track.pitch = _originalPitch;
        Track.panStereo = _originalStereoPan;
        Track.spatialBlend = _originalSpacialBlend;
        Track.reverbZoneMix = _originalReverbZoneMix;
        Track.dopplerLevel = _originalDopplerLevel;
        Track.spread = _originalSpread;
        Track.minDistance = _originalMinDistance;
        Track.maxDistance = _originalMaxDistance;
        
        // Restore the volume to what it was when the effect was applied
        Track.volume = _volumeWhenEffectApplied;
        
        Debug.Log($"Reset effects for {Track.name} - Restored Volume to: {Track.volume}");
    }
}