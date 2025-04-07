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
        //using player pref to save the all setting changes acorss diff scenes
        //volume//////
        float saved_volume = PlayerPrefs.GetFloat("Volume", 1f);
        set_volume(saved_volume);
        volume_slider.value = saved_volume;

        //quality/////
        int saved_quality = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
        set_quality(saved_quality);

        //fullscreen//////
        bool saved_fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        set_fullscreen(saved_fullscreen);

        //resolutions////////
        resolutions = Screen.resolutions;
        resolution_dropdown.ClearOptions();
        //turn array of resolutions into formatetd strings
        List<string> options = new List<string>();
        //save
        int saved_resolutionIndex = PlayerPrefs.GetInt("Resolution", 0);
        set_resolution(saved_resolutionIndex);


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
        resolution_dropdown.value = saved_resolutionIndex;
        resolution_dropdown.RefreshShownValue();
    }

    public void set_volume(float volume)
    {
        //convert volume values (0-1) to decibels
        //0 dB = full volume
        //-80 dB = silent
        float dB = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20;
        audio_mixer.SetFloat("MasterVolume", dB);
        PlayerPrefs.SetFloat("Volume", volume);
    }

    public void set_quality (int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("GraphicsQuality", qualityIndex);
    }

    public void set_fullscreen (bool is_fullscreen)
    {
        Screen.fullScreen = is_fullscreen;
        PlayerPrefs.SetInt("Fullscreen", is_fullscreen ? 1 : 0);
    }

    public void set_resolution (int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("Resolution", resolutionIndex);
    }
}
