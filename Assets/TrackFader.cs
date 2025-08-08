using System;
using System.Collections;
using UnityEngine;
using static IMusicTransition;

[System.Serializable]
public class TrackFader : IMusicTransition
{
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    public IEnumerator Execute(AudioSource ackeringTrack, AudioSource thisTrack, SectionOfAnkeringTrack sectionOfAnkeringTrack, bool MatchAnkeringTrackTime, bool FadeingIn, float StartOffsetSeconds, Action onComplete)
    {
        if (ackeringTrack == null || !ackeringTrack.isPlaying)
        {
            Debug.LogError("No current clip is playing. DelayTransition will not work.");
            yield break;
        }

        // anker transition at end of song
        if (sectionOfAnkeringTrack != SectionOfAnkeringTrack.OnTrigger)
        {
            if (sectionOfAnkeringTrack == SectionOfAnkeringTrack.TrackStart)
            {
                if (StartOffsetSeconds < 0)
                {
                    Debug.LogWarning("Acker is track start but has a negative offset");
                    yield break;
                }
                yield return new WaitUntil(() => ackeringTrack.time == StartOffsetSeconds);
            }
            else if (sectionOfAnkeringTrack == SectionOfAnkeringTrack.TrackMiddle)
            {
                yield return new WaitUntil(() => ackeringTrack.time == ackeringTrack.clip.length + StartOffsetSeconds);
            }
            else if (sectionOfAnkeringTrack == SectionOfAnkeringTrack.TrackEnd)
            {
                if (StartOffsetSeconds <= 0)
                {
                    yield return new WaitUntil(() => ackeringTrack.time == ackeringTrack.clip.length + StartOffsetSeconds);
                }
                else
                {
                    //then wait untill delaySeconds after ankering clip finishes
                    ackeringTrack.loop = false;
                    yield return new WaitUntil(() => !ackeringTrack.isPlaying);
                    yield return new WaitForSeconds(StartOffsetSeconds);
                }
            }
        }
        else //anker transition around right when the triger is activated
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
            Coroutine fadeIn = MusicManager.Instance.StartCoroutine(FadeIn(ackeringTrack, thisTrack, MatchAnkeringTrackTime));
            yield return fadeIn;
        }
        else
        {
            Coroutine fadeOut = MusicManager.Instance.StartCoroutine(FadeOut(thisTrack));
            yield return fadeOut;
        }
        onComplete?.Invoke();
    }

    private IEnumerator FadeIn(AudioSource ackeringTrack, AudioSource thisTrack, bool MatchAnkeringTrackTime)
    {
        float _fadeInDuration = fadeCurve.keys[fadeCurve.length - 1].time;
        thisTrack.volume = 0f;
        thisTrack.loop = true;
        thisTrack.Play();
        if (MatchAnkeringTrackTime) thisTrack.time = ackeringTrack.time;

        float time = 0f;
        while (time < _fadeInDuration)
        {
            time += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(time / _fadeInDuration);
            thisTrack.volume = fadeCurve.Evaluate(normalizedTime);
            yield return null;
        }
    }

    private IEnumerator FadeOut(AudioSource thisTrack)
    {
        float _fadeOutDuration = fadeCurve.keys[fadeCurve.length - 1].time;
        float startVolume = thisTrack.volume;
        float time = 0f;
        while (time < _fadeOutDuration && thisTrack.isPlaying)
        {
            time += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(time / _fadeOutDuration);
            thisTrack.volume = startVolume * fadeCurve.Evaluate(normalizedTime);
            yield return null;
        }
        thisTrack.volume = 0f;
        thisTrack.loop = false;
        thisTrack.Stop();
        
    }

}