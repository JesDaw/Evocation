using System.Collections.Generic;
using UnityEngine;
using static IMusicTransition;

public class MusicManager : MonoBehaviour
{
    AudioSource _AnchoringTrack;
    List<AudioSource> _currentTracks = new List<AudioSource>();
    [SerializeField] public List<AudioSource> Tracks = new List<AudioSource>();
    
    Dictionary<AudioSource, bool> _trackFadingStates = new Dictionary<AudioSource, bool>();
    List<AudioEffectEditor> _currentEffects = new List<AudioEffectEditor>();
    [SerializeField] event System.Action<AudioSource> OnTrackFinished;


    void Start()
    {
        foreach (AudioSource track in Tracks)
        {
            _trackFadingStates[track] = false;
        }

/*        Debug.Log("Track list:");
        foreach (AudioSource track in Tracks)
        {
            Debug.Log(track.clip.name);
        } */
    }

    void Update()
{
    AudioSource longestTrack = null;
    float longestRemainingTime = -1f;

    foreach (var track in _currentTracks)
    {
        if (track.isPlaying && track.clip != null)
        {
            float remainingTime = track.clip.length - track.time;
            if (remainingTime > longestRemainingTime)
            {
                longestRemainingTime = remainingTime;
                longestTrack = track;
            }
        }
    }

    _AnchoringTrack = longestTrack;
}

    void CurrentTrackCleanUp()
    {
        foreach (AudioEffectEditor audioEffectEditoreffect in _currentEffects)
        {
            if (!audioEffectEditoreffect.Track.isPlaying)
            {
                audioEffectEditoreffect.ResetEffects();
            }
        }
        _currentTracks.RemoveAll(track => !track.isPlaying);

        Debug.Log($"Current tracks: {_currentTracks.Count}, Anchoring track: {(_AnchoringTrack ? _AnchoringTrack.name : "None")}");
        foreach (AudioSource audioSourcetrack in Tracks)
        {
            if (!audioSourcetrack.isPlaying)
            {
                audioSourcetrack.loop = true;
                audioSourcetrack.volume = 0f;
            }
        }
    }

    public void Fade(AudioSource track, AnimationCurve fadeCurve, SectionOfAnchoringTrack sectionOfAnchoringTrack, bool matchAnchoringTrackTime, bool fadingIn, float startOffsetSeconds, System.Action onComplete)
    {
        if (_trackFadingStates.ContainsKey(track) && _trackFadingStates[track])
        {
            Debug.LogWarning($"Track {track.name} is already fading. Ignoring new fade request.");
            return;
        }

        var newFader = new TrackFader();
        _trackFadingStates[track] = true; 
        
        StartCoroutine(newFader.Execute(_AnchoringTrack, track, fadeCurve, sectionOfAnchoringTrack, matchAnchoringTrackTime, fadingIn, startOffsetSeconds, () =>
        {
            _trackFadingStates[track] = false;

            if (fadingIn && !_currentTracks.Contains(track))
            {
                _currentTracks.Add(track);
                if (_AnchoringTrack == null)
                {
                    _AnchoringTrack = track;
                    //Debug.Log($"Set anchoring track to: {track.name}");
                }
            }

            else if (!fadingIn && _currentTracks.Contains(track))
            {
                _currentTracks.Remove(track);
                track.Stop();
            }
            
            onComplete?.Invoke();
            CurrentTrackCleanUp();
        }));
    }

    public void FadeOutAllTracks(SectionOfAnchoringTrack section, AnimationCurve fadeOutCurve, float startOffsetSeconds, System.Action onComplete)
    {
        int tracksToFadeCount = _currentTracks.Count;
        int fadeCompleteCount = 0;
        
        if (tracksToFadeCount == 0)
        {
            onComplete?.Invoke();
            return;
        }

        List<AudioSource> tracksToFade = new List<AudioSource>(_currentTracks);
        
        foreach (AudioSource track in tracksToFade)
        {
            if (_trackFadingStates.ContainsKey(track) && _trackFadingStates[track])
            {
                tracksToFadeCount--;
                continue;
            }

            var newFader = new TrackFader();
            _trackFadingStates[track] = true;
            
            StartCoroutine(newFader.Execute(_AnchoringTrack, track, fadeOutCurve, section, false, false, startOffsetSeconds, () =>
            {
                _trackFadingStates[track] = false;
                if (_currentTracks.Contains(track))
                {
                    _currentTracks.Remove(track);
                    track.Stop();
                }
                
                fadeCompleteCount++;
                if (fadeCompleteCount >= tracksToFadeCount)
                {
                    onComplete?.Invoke();
                    CurrentTrackCleanUp();
                }
            }));
        }
    }

    public void ApplyTrackEffects(AudioSource sampleSource)
    {
        ResetAllPlayingTrackEffects();
        
        foreach (AudioSource track in _currentTracks)
        {
            var effectEditor = new AudioEffectEditor(track);
            effectEditor.ApplyEffects(sampleSource);
            _currentEffects.Add(effectEditor);
        }
    }

    public void ResetAllPlayingTrackEffects()
    {
        foreach (AudioEffectEditor effect in _currentEffects)
        {
            effect.ResetEffects();
        }
        _currentEffects.Clear(); 
    }

    public void ResetEffects(AudioEffectEditor effect)
    {
        effect.ResetEffects();
        _currentEffects.Remove(effect);
    }

    public void StopCurrentTracks()
    {
        foreach (AudioSource track in _currentTracks)
        {
            track.Stop();
        }
        _currentTracks.Clear();
        _AnchoringTrack = null;
    }
    
    public void PauseCurrentTracks()
    {
        foreach (AudioSource track in _currentTracks)
        {
            track.Pause();
        }
    }
    
    public void PlayCurrentTracks()
    {
        foreach (AudioSource track in _currentTracks)
        {
            track.Play();
        }
    }

    public static MusicManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ResetAllPlayingTrackEffects();
            Instance = null;
        }
    }
}