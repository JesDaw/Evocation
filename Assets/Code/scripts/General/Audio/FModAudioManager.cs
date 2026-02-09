using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

public class FModAudioManager : MonoBehaviour
{
    public static FModAudioManager instance { get; private set; }
    Dictionary<string, EventReference> soundDictionary = new Dictionary<string, EventReference>();
    [SerializeField] GameVolumeSO gameVolumeSO;
    Bus masterBuss;
    Bus MusicBuss;
    Bus sfxBuss;


    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (gameVolumeSO == null) Debug.LogWarning($"[VolumeSlider] gameVolumeSO not assigned on {gameObject.name}");


        masterBuss = RuntimeManager.GetBus("bus:/");
        MusicBuss = RuntimeManager.GetBus("bus:/Music");
        sfxBuss = RuntimeManager.GetBus("bus:/SoundEffects");

    }
    void Start()
    {
        //UI
        soundDictionary.Add("menuClick", FModEvents.instance.menuClick);
        soundDictionary.Add("dialogueType", FModEvents.instance.menuClick);
        // Character select
        soundDictionary.Add("showCharacterInfo", FModEvents.instance.showCharacterInfo);
        soundDictionary.Add("removeCharacterFromParty", FModEvents.instance.removeCharacterFromParty);
        soundDictionary.Add("addCharacterToParty", FModEvents.instance.addCharacterToParty);
        // navigating menus
        soundDictionary.Add("pauseGame", FModEvents.instance.pauseGame);
        soundDictionary.Add("resumeGame", FModEvents.instance.unpauseGame);
        soundDictionary.Add("openCharacterSelect", FModEvents.instance.openCharacterSelect);
        soundDictionary.Add("closeCharacterSelect", FModEvents.instance.closeCharacterSelect);
        //starting the battle
        soundDictionary.Add("engageInBattle", FModEvents.instance.engageInBattle);
        soundDictionary.Add("backToScouting", FModEvents.instance.backToScouting);

        
        // gameplay
        soundDictionary.Add("spawnTroop", FModEvents.instance.spawnTroop);
        soundDictionary.Add("attack", FModEvents.instance.attack);
        soundDictionary.Add("takeDamage", FModEvents.instance.takeDamage);
        soundDictionary.Add("knockback", FModEvents.instance.knockback);
        soundDictionary.Add("die", FModEvents.instance.die);
        soundDictionary.Add("claimLocation", FModEvents.instance.claimLocation);
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

    void Update()
    {
        masterBuss.setVolume(gameVolumeSO.MasterVolume);
        MusicBuss.setVolume(gameVolumeSO.MusicVolume);
        sfxBuss.setVolume(gameVolumeSO.SFXVolume);
    }
}