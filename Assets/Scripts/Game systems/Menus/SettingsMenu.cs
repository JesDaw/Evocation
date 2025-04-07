using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public Slider volume_slider;
    public AudioMixer audio_mixer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //using player pref to save the volume setting acorss diff scenes
        float saved_volume = PlayerPrefs.GetFloat("Volume", 1f);
        set_volume(saved_volume);
        volume_slider.value = saved_volume;
    }

    public void set_volume(float volume)
    {
        //convert volume values (0-1) to decibels
        //0 dB = full volume
        //-80 dB = silent
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20;
        audio_mixer.SetFloat("MasterVolume", dB);
    }

    public void set_quality (int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }
}
