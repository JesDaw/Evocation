using UnityEngine;

public class AnimationEventsController : MonoBehaviour
{
    bool shouldAttack;
    void attackAnimationEnd()
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
