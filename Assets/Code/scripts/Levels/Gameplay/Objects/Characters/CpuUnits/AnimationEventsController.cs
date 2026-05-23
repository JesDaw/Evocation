using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AnimationEventsController : MonoBehaviour
{
    [SerializeField] Stats CharacterStats;
    [SerializeField] AttackSoundType attackSoundType;
    [SerializeField] StudioEventEmitter stepWoodStudioEventEmitter;
    [SerializeField] StudioEventEmitter stepStoneStudioEventEmitter;
    [SerializeField] StudioEventEmitter swingStudioEventEmitter;
    [Range(0f,1f)]    
    bool shouldAttack;
    void Start()
    {
 
    }
    public void attackAnimationEnd()
    {
        shouldAttack = true;
    }
    public void ResetAttackSignal()
    {
        shouldAttack = false;
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
                    swingStudioEventEmitter.Play();
                    break;
                case AttackSoundType.Stab:
                    swingStudioEventEmitter.Play();  
                    break;
                case AttackSoundType.Smash:
                    swingStudioEventEmitter.Play();
                    break;
            }
        }        
        ResetAttackSignal();
        return true;
    }
    
    void OnStep()
    {
        if (FModAudioManager.instance == null)
        {
            Debug.LogWarning("No audio manager");
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, LayerMask.GetMask("Ground/TopLane", "Ground/MidLane", "Ground/BotLane"));

        if (hit.collider != null)
        {
            switch (hit.collider.tag)
            {
                case "Ground/wood":
                    stepWoodStudioEventEmitter.Play();
                    break;
                case "Ground/stone":
                    stepStoneStudioEventEmitter.Play();
                    break;
                default:
                    stepStoneStudioEventEmitter.Play();
                    break;
            }
        }
        else
        {
        }
    }
}

public enum AttackSoundType{ Slash, Stab, Smash}