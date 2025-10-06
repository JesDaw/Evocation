using UnityEngine;

public class TrackFadeCallAssister : MonoBehaviour
{
    [SerializeField] SoundTrackfadeInCaller trackToCall;
    [SerializeField] AudioSource RefTrack;
    [SerializeField] float timeToCall = 0f;
    
    [SerializeField] internal UltEvents.UltEvent CallFadeTracks;
    bool fadeCalled = false;

    void Update()
    {
       // Debug.Log($"{!fadeCalled} && {RefTrack.time} >= {timeToCall}: {!fadeCalled && RefTrack.time == timeToCall}");
        if (!fadeCalled && RefTrack.time >= timeToCall)
        {
            //Debug.Log("TrackFadeCallAssister now invoking event!");
            CallFadeTracks.Invoke();
            fadeCalled = true;
        }
    }
}
