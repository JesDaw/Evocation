using UnityEngine;
using FMODUnity;
using System.Collections.Generic;

public class FModAudioManager : MonoBehaviour
{
    public static FModAudioManager instance { get; private set; }
    private Dictionary<string, EventReference> soundDictionary = new Dictionary<string, EventReference>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Populate sound dictionary
        soundDictionary.Add("menuClick", FModEvents.instance.menuClick);
        // Add more sounds as needed, e.g., soundDictionary.Add("buttonHover", FModEvents.instance.buttonHover);
    }

    public void PlayOneShot(EventReference sound, UnityEngine.Vector3 worldPosition)
    {
        RuntimeManager.PlayOneShot(sound, worldPosition);
    }

    public void PlayOneShot(EventReference sound)
    {
        RuntimeManager.PlayOneShot(sound);
    }

    public void PlaySoundByName(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out EventReference sound))
        {
            PlayOneShot(sound);
        }
        else
        {
            Debug.LogWarning($"Sound '{soundName}' not found in dictionary.");
        }
    }
}