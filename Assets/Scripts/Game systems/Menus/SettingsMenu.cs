using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

public class SettingsMenu : MonoBehaviour
{
    public Slider volume_slider;
    public TMPro.TMP_Dropdown resolution_dropdown;
    public AudioMixer audio_mixer;

    Resolution[] resolutions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //using player pref to save the volume setting acorss diff scenes
        //volume//////
        float saved_volume = PlayerPrefs.GetFloat("Volume", 1f);
        set_volume(saved_volume);
        volume_slider.value = saved_volume;

        //quality/////
        int saved_quality = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
        set_quality(saved_quality);

        //resolutions////////
        resolutions = Screen.resolutions;
        resolution_dropdown.ClearOptions();
        //turn array of resolutions into formatetd strings
        List<string> options = new List<string>();

        int current_res = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + "x" + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                current_res = i;
            }
        }
        resolution_dropdown.AddOptions(options);
        resolution_dropdown.value = current_res;
        resolution_dropdown.RefreshShownValue();
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

    public void set_fullscreen (bool is_fullscreen)
    {
        Screen.fullScreen = is_fullscreen;
    }

    public void set_resolution (int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}
