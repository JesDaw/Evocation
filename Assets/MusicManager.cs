using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static IMusicTransition;

public interface IMusicTransition
{
    public enum SectionOfAnkeringTrack
    {
        TrackStart,
        TrackMiddle,
        TrackEnd,
        OnTrigger,
    };
    IEnumerator Execute(AudioSource ackeringTrack, AudioSource thisTrack, SectionOfAnkeringTrack sectionOfAnkeringTrack, bool MatchAnkeringTrackTime, bool FadeIn, float StartOffsetSeconds, System.Action onComplete);
}
public class MusicManager : MonoBehaviour
{
    [SerializeField] AudioSource _ackeringTrack;
    [SerializeField] public List<AudioSource> Tracks = new List<AudioSource>();
    [SerializeField] List<AudioSource> _currentTracks = new List<AudioSource>();
    TrackFader trackFader;

    void Fade(AudioSource track, SectionOfAnkeringTrack section, bool MatchAnkeringTrackTime, bool mutedTrack, bool FadeingIn, float StartOffsetSeconds, System.Action onComplete)
    {
        trackFader.Execute(_ackeringTrack, track, section, MatchAnkeringTrackTime, FadeingIn, StartOffsetSeconds, onComplete);
        //clean up current tracks list
        _ackeringTrack = _currentTracks[0];

    }

    public void StopCurrentTracks()
    {
        foreach (AudioSource track in _currentTracks)
        {
            track.Stop();
        }
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

    public void EditTrackEffects()
    {
        foreach (AudioSource track in _currentTracks)
        {

        } 
    }

    public void ResetTrackEffects()
    {
        foreach (AudioSource track in _currentTracks)
        {

        } 
    }

    

    public static MusicManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Optional: enforce only one
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional
    }

}
