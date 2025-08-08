using UnityEngine;

public class AudioEffectEditor : MonoBehaviour
{
    ScriptableAudioEffects scriptableAudioEffects;
    AudioSource Track;
    public AudioEffectEditor(AudioSource Track)
    {
        this.Track = Track;
        _originalpriority = Track.priority;
        _originalVolume = Track.volume;
        _originalPitch = Track.pitch;
        _originalStereoPan = Track.panStereo;
        _originalSpacialBlend = Track.spatialBlend;
        _originalReverbZoneMix = Track.reverbZoneMix;
        _originalDopplerLevel = Track.dopplerLevel;
        _originalSpread = Track.spread;
        _originalMinDistance = Track.minDistance;
        _originalMaxDistance = Track.maxDistance;
    }

    public void ResetEffects()
    {
        
    }



    int _originalpriority;
    float _originalVolume;
    float _originalPitch;
    float _originalStereoPan;
    float _originalSpacialBlend;
    float _originalReverbZoneMix;
    float _originalDopplerLevel;
    float _originalSpread;
    float _originalMinDistance;
    float   _originalMaxDistance;
    
}
