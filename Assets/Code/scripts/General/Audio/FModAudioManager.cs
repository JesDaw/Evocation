using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

public class FModAudioManager : MonoBehaviour 
{
    Dictionary<string, EventReference> soundDictionary = new Dictionary<string, EventReference>();
    [SerializeField] GameVolumeSO gameVolumeSO;
    Bus masterBuss;
    Bus MusicBuss;
    Bus sfxBuss;

    public static FModAudioManager instance { get; private set; }

    void Awake() 
    {
        if (instance != null && instance != this) 
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (gameVolumeSO == null) 
            Debug.LogWarning($"[VolumeSlider] gameVolumeSO not assigned on {gameObject.name}");

        masterBuss = RuntimeManager.GetBus("bus:/");
        MusicBuss = RuntimeManager.GetBus("bus:/Music");
        sfxBuss = RuntimeManager.GetBus("bus:/SoundEffects");
    }

    void Start() 
    {
        // UI
        soundDictionary.Add("menuClick", FModEvents.instance.menuClick);
        soundDictionary.Add("dialogueType", FModEvents.instance.menuClick);
        soundDictionary.Add("back", FModEvents.instance.back);
        
        // Character select
        soundDictionary.Add("showCharacterInfo", FModEvents.instance.showCharacterInfo);
        soundDictionary.Add("removeCharacterFromParty", FModEvents.instance.removeCharacterFromParty);
        soundDictionary.Add("addCharacterToParty", FModEvents.instance.addCharacterToParty);
        
        // Characters
        soundDictionary.Add("WolfRunner", FModEvents.instance.WolfRunner);
        soundDictionary.Add("WolfHammer", FModEvents.instance.WolfHammer);
        soundDictionary.Add("WolfRider", FModEvents.instance.WolfRider);
        soundDictionary.Add("HoodedGuy", FModEvents.instance.HoodedGuy);
        soundDictionary.Add("WolfMage", FModEvents.instance.WolfMage);
        
        // navigating menus
        soundDictionary.Add("pauseGame", FModEvents.instance.pauseGame);
        soundDictionary.Add("resumeGame", FModEvents.instance.unpauseGame);
        soundDictionary.Add("openCharacterSelect", FModEvents.instance.openCharacterSelect);
        soundDictionary.Add("closeCharacterSelect", FModEvents.instance.closeCharacterSelect);
        
        // starting the battle
        soundDictionary.Add("engageInBattle", FModEvents.instance.engageInBattle);
        soundDictionary.Add("backToScouting", FModEvents.instance.backToScouting);
        
        // gameplay
        soundDictionary.Add("spawnTroop", FModEvents.instance.spawnTroop);
        soundDictionary.Add("claimLocation", FModEvents.instance.claimLocation);
        
        // footsteps
        soundDictionary.Add("walkWood", FModEvents.instance.walkWood);
        soundDictionary.Add("walkstone", FModEvents.instance.walkstone);
        
        // melee combat
        soundDictionary.Add("attack", FModEvents.instance.attack);
        soundDictionary.Add("takeDamage", FModEvents.instance.takeDamage);
        soundDictionary.Add("knockback", FModEvents.instance.knockback);
        soundDictionary.Add("die", FModEvents.instance.die);
        
        // Projectiles
        soundDictionary.Add("shootFireball", FModEvents.instance.shootFireball);
        soundDictionary.Add("fireballHit", FModEvents.instance.fireballHit);
    }

    // NEW OVERLOAD: Allows calling dictionary strings with custom parameters
    public void PlaySoundByName(string soundName, Vector3 position, float minDistance, float maxDistance, string paramName, float paramValue)
    {
        if (soundDictionary.TryGetValue(soundName, out EventReference sound)) 
        {
            PlayOneShot(sound, position, minDistance, maxDistance, paramName, paramValue);
        } 
        else 
        {
            Debug.LogWarning($"Sound '{soundName}' not found in dictionary.");
        }
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

    public void PlayOneShot(EventReference sound, Vector3 position, float minDistance, float maxDistance, string paramName, float paramValue) 
    {
        
        FMOD.Studio.EventInstance instance = RuntimeManager.CreateInstance(sound);
        FMOD.ATTRIBUTES_3D attributes = RuntimeUtils.To3DAttributes(position);
        instance.set3DAttributes(attributes);
        instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MINIMUM_DISTANCE, minDistance);
        instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, maxDistance);
        instance.setParameterByName(paramName, paramValue);
        instance.start();
        instance.release();
    }

    public void PlayOneShot(EventReference sound, UnityEngine.Vector3 worldPosition) 
    {
        RuntimeManager.PlayOneShot(sound, worldPosition);
    }

    public void PlayOneShot(EventReference sound) 
    {
        RuntimeManager.PlayOneShot(sound);
    }

    void Update() 
    {
        masterBuss.setVolume(gameVolumeSO.MasterVolume);
        MusicBuss.setVolume(gameVolumeSO.MusicVolume);
        sfxBuss.setVolume(gameVolumeSO.SFXVolume);
    }
}
