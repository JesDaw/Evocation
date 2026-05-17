using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AnimationEventsController : MonoBehaviour
{
    [SerializeField] Stats CharacterStats;
    [SerializeField] AttackSoundType attackSoundType;
    [SerializeField] StudioEventEmitter stepStudioEventEmitter;
    [SerializeField] StudioEventEmitter swingStudioEventEmitter;
    [SerializeField] StudioEventEmitter gettingHitEventEmitter;
    [Range(0f,1f)]    
    bool shouldAttack;
    void Start()
    {
        if (stepStudioEventEmitter != null)
        {
            stepStudioEventEmitter.EventReference = FModEvents.instance.walkWood;
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
                    swingStudioEventEmitter.EventReference = FModEvents.instance.attack;
                    break;
                case AttackSoundType.Stab:
                    swingStudioEventEmitter.EventReference = FModEvents.instance.attack;   
                    break;
                case AttackSoundType.Smash:
                    swingStudioEventEmitter.EventReference = FModEvents.instance.attack; 
                    break;
            }
        }
        Debug.Log("playing attacking");
        swingStudioEventEmitter.Play();
        
        shouldAttack = false;
        return true;
    }
    public void DamageSound()
    {
        gettingHitEventEmitter.EventReference = FModEvents.instance.takeDamage; 
        gettingHitEventEmitter.Play();
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
                    stepStudioEventEmitter.EventReference = FModEvents.instance.walkWood;
                    break;
                case "Ground/stone":
                    stepStudioEventEmitter.EventReference = FModEvents.instance.walkstone;
                    break;
                default:
                    stepStudioEventEmitter.EventReference = FModEvents.instance.walkWood;
                    break;
            }
        }
        else
        {
            stepStudioEventEmitter.EventReference = FModEvents.instance.walkWood;
        }
        
        stepStudioEventEmitter.Play();
    }
}

public enum AttackSoundType{ Slash, Stab, Smash}