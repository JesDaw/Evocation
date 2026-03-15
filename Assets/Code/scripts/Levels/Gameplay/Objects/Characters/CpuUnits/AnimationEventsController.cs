using UnityEngine;

public class AnimationEventsController : MonoBehaviour
{
    bool shouldAttack;
    public void attackAnimationEnd()
    {
        shouldAttack = true;
    }
    public bool ShouldAttack()
    {
        if(!shouldAttack) return false;
        shouldAttack = false;
        return true;
    }
    void OnStep()
    {
        if (FModAudioManager.instance == null) 
        {
            //Debug.LogWarning("No audio manager");
            return;
        }
        FModAudioManager.instance.PlaySoundByName("attack");
        // This could definitely be done in a much better way, it's not even modular and plays this single sound. ^^
        // I would make it randomize the pitch, but I do not know where the "FModAudioManager" stores sounds.. - Chris S.
    }
}
