using UnityEngine;
using UnityEngine.Audio;
using System;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();

            sound.source.clip = sound.clip;

            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;

            sound.source.loop = sound.loop;

            sound.source.outputAudioMixerGroup = sound.mixer_group;
        }
    }

    public void Play(string name)
    {
        Sound sound = Array.Find(sounds, sound => sound.name == name);
        if (sound != null) sound.source.Play();
        else Debug.LogError("Sound to play not found: " + name);
    }

    public void Stop (string name)
    {
        Sound sound = Array.Find(sounds, sound => sound.name == name);
       if (sound != null) sound.source.Stop();
        else Debug.LogError("Sound to stop not found: " + name);
        
    }

}
