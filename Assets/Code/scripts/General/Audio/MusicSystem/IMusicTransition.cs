using System.Collections;
using UnityEngine;

public interface IMusicTransition
{
    public enum SectionOfAnchoringTrack
    {
        TrackStart,
        TrackMiddle,
        TrackEnd,
        OnTrigger,
    };
    IEnumerator Execute(AudioSource ackeringTrack, AudioSource thisTrack, AnimationCurve fadeCurve, SectionOfAnchoringTrack sectionOfAnchoringTrack, bool MatchAnchoringTrackTime, bool FadeIn, float StartOffsetSeconds, System.Action onComplete);
}