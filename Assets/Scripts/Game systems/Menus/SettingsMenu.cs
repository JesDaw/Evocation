using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour 
{
    public AudioMixer audioMixer;

    // Slider needs graphic to work but I couldn't figure out how to
    // create a graphic for it
    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("volume", volume);
    }

}
