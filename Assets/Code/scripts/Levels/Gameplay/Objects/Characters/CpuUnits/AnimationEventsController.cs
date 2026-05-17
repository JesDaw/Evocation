using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AnimationEventsController : MonoBehaviour
{
    [SerializeField] Stats CharacterStats;
    [SerializeField] AttackSoundType attackSoundType;
    [SerializeField] StudioEventEmitter StudioEventEmitter;
    [Range(0f,1f)]    
    bool shouldAttack;
    void Start()
    {
        if (StudioEventEmitter != null)
        {
            StudioEventEmitter.EventReference = FModEvents.instance.walkWood;
        }
        else
        {
            
        }
    }
    public void attackAnimationEnd()
    {
        shouldAttack = true;
    }
    public bool ShouldAttack()
    {
        if(!shouldAttack) return false;
        if (CharacterStats._IsProjectile) FModAudioManager.instance.PlaySoundByName("shootFireball");
        else
        {
            switch (attackSoundType)
            {
                case AttackSoundType.Slash:
                    StudioEventEmitter.EventReference = FModEvents.instance.attack;
                    break;
                case AttackSoundType.Stab:
                    StudioEventEmitter.EventReference = FModEvents.instance.attack;   
                    break;
                case AttackSoundType.Smash:
                    StudioEventEmitter.EventReference = FModEvents.instance.attack; 
                    break;
            }
        }
        StudioEventEmitter.Play();
        
        shouldAttack = false;
        return true;
    }
    public void DamageSound()
    {
        StudioEventEmitter.EventReference = FModEvents.instance.takeDamage; 
        StudioEventEmitter.Play();
    }
    void OnStep()
    {
        if (FModAudioManager.instance == null)
        {
            Debug.LogWarning("No audio manager");
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.5f, LayerMask.GetMask("Ground/TopLane", "Ground/MidLane", "Ground/BotLane"));

        if (hit.collider != null)
        {
            switch (hit.collider.tag)
            {
                case "Ground/wood":
                    StudioEventEmitter.EventReference = FModEvents.instance.walkWood;
                    break;
                case "Ground/stone":
                    StudioEventEmitter.EventReference = FModEvents.instance.walkstone;
                    break;
                default:
                    StudioEventEmitter.EventReference = FModEvents.instance.walkWood;
                    break;
            }
        }
        else
        {
            StudioEventEmitter.EventReference = FModEvents.instance.walkWood;
        }
        Debug.Log("playing footstep");
        StudioEventEmitter.Play();
    }
}

public enum AttackSoundType{ Slash, Stab, Smash}