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
}
