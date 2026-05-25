using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AnimationEventsController : MonoBehaviour 
{
    [SerializeField] Stats CharacterStats;
    [SerializeField] AttackSoundType attackSoundType;
    
    // Attenuation and Parameter configuration
    [Header("Audio Customization")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private string parameterName = "Volume";
    [Range(0f,1f)] 
    [SerializeField] private float parameterValue = 1f;

    bool shouldAttack;

    void Start() { }

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
        
        if (CharacterStats._IsProjectile) 
        {
            FModAudioManager.instance.PlaySoundByName("shootFireball", transform.position, minDistance, maxDistance, parameterName, parameterValue);
        }
        else 
        {
            FModAudioManager.instance.PlaySoundByName("attack", transform.position, minDistance, maxDistance, parameterName, parameterValue);
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
                    FModAudioManager.instance.PlaySoundByName("walkWood", transform.position, minDistance, maxDistance, parameterName, parameterValue);
                    break;
                case "Ground/stone": 
                    FModAudioManager.instance.PlaySoundByName("walkstone", transform.position, minDistance, maxDistance, parameterName, parameterValue);
                    break;
                default: 
                    FModAudioManager.instance.PlaySoundByName("walkstone", transform.position, minDistance, maxDistance, parameterName, parameterValue);
                    break;
            }
        } 
    }
}

public enum AttackSoundType { Slash, Stab, Smash }
