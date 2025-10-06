using System;
using System.Collections;
using UnityEngine;
using static IMusicTransition;

[System.Serializable]
public class TrackFader : IMusicTransition
{
    public IEnumerator Execute(AudioSource AnchoringTrack, AudioSource thisTrack, AnimationCurve fadeCurve, SectionOfAnchoringTrack sectionOfAnchoringTrack, bool MatchAnchoringTrackTime, bool FadeingIn, float StartOffsetSeconds, Action onComplete)
    {
        if ((AnchoringTrack == null || !AnchoringTrack.isPlaying || AnchoringTrack.clip == null) && FadeingIn && (MatchAnchoringTrackTime || sectionOfAnchoringTrack != SectionOfAnchoringTrack.OnTrigger))
        {
            Debug.LogError("No current clip is playing. Fading in while Matching Anchoring track time or timing it with a certain part of the track won't work.");
            yield break;
        }

        // Anchor transition timing
        if (sectionOfAnchoringTrack != SectionOfAnchoringTrack.OnTrigger)
        {
            if (sectionOfAnchoringTrack == SectionOfAnchoringTrack.TrackStart)
            {
                if (StartOffsetSeconds < 0)
                {
                    Debug.LogWarning("Anchor is track start but has a negative offset");
                    yield break;
                }
                yield return new WaitUntil(() => AnchoringTrack.time >= StartOffsetSeconds);
            }
            else if (sectionOfAnchoringTrack == SectionOfAnchoringTrack.TrackMiddle)
            {
                yield return new WaitUntil(() => AnchoringTrack.time >= (AnchoringTrack.clip.length / 2) + StartOffsetSeconds);
            }
            else if (sectionOfAnchoringTrack == SectionOfAnchoringTrack.TrackEnd)
            {
                AnchoringTrack.loop = false;
                Debug.Log($" {AnchoringTrack} - no longer looping");
                if (StartOffsetSeconds <= 0)
                {
                    yield return new WaitUntil(() => AnchoringTrack.time >= AnchoringTrack.clip.length + StartOffsetSeconds);
                }
                else
                {
                    yield return new WaitUntil(() => !AnchoringTrack.isPlaying);
                    yield return new WaitForSeconds(StartOffsetSeconds);
                }
            }
        }
        else // Anchor transition on trigger
        {
            if (StartOffsetSeconds < 0)
            {
                Debug.LogError("Cannot start on trigger with negative delay.");
                yield break;
            }
            yield return new WaitForSeconds(StartOffsetSeconds);
        }

        if (FadeingIn)
        {
            yield return MusicManager.Instance.StartCoroutine(FadeIn(AnchoringTrack, thisTrack, fadeCurve, MatchAnchoringTrackTime));
        }
        else
        {
            yield return MusicManager.Instance.StartCoroutine(FadeOut(thisTrack, fadeCurve));
        }
        
        onComplete?.Invoke();
    }

    private IEnumerator FadeIn(AudioSource AnchoringTrack, AudioSource thisTrack, AnimationCurve fadeInCurve, bool MatchAnchoringTrackTime)
    {
        float _fadeInDuration = fadeInCurve.keys[fadeInCurve.length - 1].time;

        thisTrack.loop = true;
        thisTrack.Play();
        
        if (MatchAnchoringTrackTime && AnchoringTrack != null && AnchoringTrack.isPlaying) 
        {
            thisTrack.time = AnchoringTrack.time;
        }

        float time = 0f;
        while (time < _fadeInDuration)
        {
            time += Time.deltaTime;
            
            // Use the curve to get the volume multiplier (0-1), then multiply by original volume
            float curveValue = fadeInCurve.Evaluate(time);
            if (curveValue < 0f) curveValue = 0f;
            else if (curveValue > 1f) curveValue = 1f;
            thisTrack.volume = curveValue;
            
            //Debug.Log($"Fade In {thisTrack} - Time: {time:F2}, Curve: {curveValue:F2}, Volume: {thisTrack.volume:F2}");
            yield return null;
        }        
    }

    private IEnumerator FadeOut(AudioSource thisTrack, AnimationCurve fadeOutCurve)
    {
        float _fadeOutDuration = fadeOutCurve.keys[fadeOutCurve.length - 1].time;

        float _startVol = thisTrack.volume;

        float time = 0f;
        while (time < _fadeOutDuration)
        {
            time += Time.deltaTime;
            
            // Apply curve directly to the start volume
            float curveValue = fadeOutCurve.Evaluate(time);
            if (curveValue < 0f) curveValue = 0f;
            else if (curveValue > 1f) curveValue = 1f;
            thisTrack.volume = _startVol * curveValue;
            
            //Debug.Log($"Fade Out {thisTrack}  - Time: {time:F2}, Curve: {curveValue:F2}, Volume: {thisTrack.volume:F2}");
            yield return null;
        }
    }
}