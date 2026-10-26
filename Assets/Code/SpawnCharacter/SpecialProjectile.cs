using UnityEngine;
using UnityEngine.Events;

//not really a projectile just use it to spawn crap
public class SpecialProjectile : MonoBehaviour
{
    protected Stats attacker;
    public void SetAttacker(Stats _attacker)
    {
        attacker = _attacker;
    }
}
