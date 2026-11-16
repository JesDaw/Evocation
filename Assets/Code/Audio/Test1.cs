using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

public class Test1 : MonoBehaviour
{
    [field: SerializeField] public EventReference WolfRider { get; private set; }

    Bus masterBuss;
    Bus MusicBuss;
    Bus sfxBuss;

    public static Test1 instance { get; private set; }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        masterBuss = RuntimeManager.GetBus("bus:/");
        MusicBuss = RuntimeManager.GetBus("bus:/Music");
        sfxBuss = RuntimeManager.GetBus("bus:/SoundEffects");
    }

    public void OnEnable()
    {
        PlayOneShot();
    }
    public void PlayOneShot()
    {
        RuntimeManager.PlayOneShot(WolfRider);
        Debug.Log("SOund played");
    }
}
