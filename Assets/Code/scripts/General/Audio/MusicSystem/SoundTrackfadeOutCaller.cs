using UnityEngine;
using static IMusicTransition;

public class SoundTrackfadeOutCaller : MonoBehaviour
{
    [SerializeField] MusicManager musicManager;
    [SerializeField] bool fadeOutAllTracks = false;
    [SerializeField] [Range(0, 10)] int trackToFadeOut;
    [SerializeField] AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] SectionOfAnchoringTrack sectionOfAnchoringTrackToStartFadeOut;
    [SerializeField] float FadeOutStartOffsetSeconds;
    

    public void FadeTracksOut()
    {
        //Debug.Log("fade out called");
        if (musicManager.Tracks[trackToFadeOut] == null)
        {
            Debug.LogError("attemted to fade in a track outside of the music manager list");
            return;
        }

        if (fadeOutAllTracks)
        {
            musicManager.FadeOutAllTracks(sectionOfAnchoringTrackToStartFadeOut, fadeOutCurve, FadeOutStartOffsetSeconds, () =>
            {
                Debug.Log("Fade out all tracks completed");
                ResetManualTest();
            });
        }
        else
        {
            //Debug.Log("fading out");
            musicManager.Fade(musicManager.Tracks[trackToFadeOut], fadeOutCurve, sectionOfAnchoringTrackToStartFadeOut, false, false, FadeOutStartOffsetSeconds, () =>
            {
                //Debug.Log($"Fade out completed for {musicManager.Tracks[trackToFadeOut].clip.name}");
                ResetManualTest();
            });
        }
    }

    [SerializeField] bool ManualTest = false;
    bool _testing = false;
    
    void Start()
    {
        ResetManualTest();
    }

    void Update()
    {
        if (_testing == ManualTest) return;
        if (ManualTest && !_testing)
        {
            FadeTracksOut();
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