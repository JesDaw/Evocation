using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

public class SettingsMenu : MonoBehaviour
{
    public Slider master_slider;
    public Slider music_slider;
    public Slider sfx_slider;
    public TMPro.TMP_Dropdown resolution_dropdown;
    public AudioMixer audio_mixer;

    Resolution[] resolutions;

    // These Variable and function names are not following our naming conventions
    //also what the heck is PlayerPrefs? I dont see that object anywhere
    void Awake() 
    {
        load_settings();
    }

    void Start()
    {
        load_settings();
    }

    void load_settings() // this function is pretty long can it be split into smaller functions?
    {

        float master_volume = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        float music_volume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfx_volume = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        set_master(master_volume);
        set_music(music_volume);
        set_sfx(sfx_volume);

        master_slider.value = master_volume;
        music_slider.value = music_volume;
        sfx_slider.value = sfx_volume;


        int saved_quality = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
        set_quality(saved_quality);

        bool saved_fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        set_fullscreen(saved_fullscreen);

        if (resolution_dropdown != null)
        {
            resolutions = Screen.resolutions;
            resolution_dropdown.ClearOptions();
            List<string> options = new List<string>();
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
    }

    //convert volume values (0-1) to decibels
    //0 dB = full volume
    //-80 dB = silent
    //fucntions for each volume slider
    public void set_master(float volume)
    {
        audio_mixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void set_music(float volume)
    {
        audio_mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void set_sfx(float volume)
    {
        audio_mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    public void set_quality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("GraphicsQuality", qualityIndex);
    }

    public void set_fullscreen(bool is_fullscreen)
    {
        Screen.fullScreen = is_fullscreen;
        PlayerPrefs.SetInt("Fullscreen", is_fullscreen ? 1 : 0);
    }

    public void set_resolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("Resolution", resolutionIndex);
    }
}
