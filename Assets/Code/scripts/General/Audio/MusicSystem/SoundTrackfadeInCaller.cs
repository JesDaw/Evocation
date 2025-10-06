using UnityEngine;
using static IMusicTransition;

public class SoundTrackfadeInCaller : MonoBehaviour
{
    [SerializeField] MusicManager musicManager;
    [SerializeField] [Range(0, 10)] int TrackToFadeIn;
    [SerializeField] AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] SectionOfAnchoringTrack sectionOfAnchoringTrackToStartFadeIn;
    [SerializeField] bool MatchAnchoringTrackTime;
    [SerializeField] float FadeInStartOffsetSeconds;
    


    public void FadeTracksIn()
    {
        if (musicManager.Tracks[TrackToFadeIn] == null)
        {
            Debug.LogError("attemted to fade in a track outside of the music manager list");
            return;
        }

        musicManager.Fade(musicManager.Tracks[TrackToFadeIn], fadeInCurve, sectionOfAnchoringTrackToStartFadeIn, MatchAnchoringTrackTime, true, FadeInStartOffsetSeconds, () =>
        {
            //Debug.Log($"Fade in completed for {musicManager.Tracks[TrackToFadeIn].clip.name}");
            ResetManualTest();
        });

        
    }

    [SerializeField] bool ManualTest = false;
    bool _testing = false; // Make visible in inspector
    
    void Start()
    {
        ResetManualTest();
    }
    
    void Update()
    {
        if (_testing == ManualTest) return;
        if (ManualTest && !_testing)
        {
            FadeTracksIn();
            _testing = true;
        }
        else if (!ManualTest && _testing)
        {
            _testing = false;
        }
    }

    public void ResetManualTest()
    {
        ManualTest = false;
        _testing = false;
    }
}