using UnityEngine;
using FMODUnity;

public class FModAudioManager : MonoBehaviour
{
    public static FModAudioManager instance { get; private set; } // lowercase

    void Awake()
    {
        if (instance != null && instance != this) // both lowercase
        {
            Destroy(gameObject);
            return;
        }

        instance = this; // lowercase
        DontDestroyOnLoad(gameObject);
    }

    public void PlayOneShot(EventReference sound, UnityEngine.Vector3 worldPosition)
    {
        RuntimeManager.PlayOneShot(sound, worldPosition);
    }

    public void PlayOneShot(EventReference sound)
    {
        RuntimeManager.PlayOneShot(sound);
    }
}