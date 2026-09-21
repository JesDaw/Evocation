using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [SerializeField] GameVolumeSO gameVolumeSO;
    enum VolumeType
    {
        Master,
        Music,
        SFX
    }

    [SerializeField]
    VolumeType volumeType;
    [SerializeField] Slider slider;
    [SerializeField] bool DebugLogs;

    void Awake()
    {
        if(TryGetComponent<Slider>(out Slider sl)) slider = sl;
        else if (slider == null) Debug.LogWarning($"[VolumeSlider] No slider found on {gameObject.name}");

        if (gameVolumeSO == null) Debug.LogWarning($"[VolumeSlider] gameVolumeSO not assigned on {gameObject.name}");
        slider.value = gameVolumeSO.MasterVolume;
        slider.value = gameVolumeSO.MusicVolume;
        slider.value = gameVolumeSO.SFXVolume;
    }

    void OnEnable()
    {
        switch (volumeType)
        {
            case VolumeType.Master:
                slider.value = gameVolumeSO.MasterVolume;
                break;
            case VolumeType.Music:
                slider.value = gameVolumeSO.MusicVolume;
                break;
            case VolumeType.SFX:
                slider.value = gameVolumeSO.SFXVolume;
                break;
            default:
                Debug.LogWarning("Unexpected Volume Type: " + volumeType);
                break;
        }
    }


    void Update()
    {
        switch (volumeType)
        {
            case VolumeType.Master:
                gameVolumeSO.MasterVolume = slider.value;
                break;
            case VolumeType.Music:
                gameVolumeSO.MusicVolume = slider.value;
                break;
            case VolumeType.SFX:
                gameVolumeSO.SFXVolume = slider.value;
                break;
            default:
                Debug.LogWarning("Unexpected Volume Type: " + volumeType);
                break;
        }
    }

    public void OnSliderValueChanged()
    {
        
    }
}