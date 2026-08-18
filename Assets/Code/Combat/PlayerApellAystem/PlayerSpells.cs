using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PlayerSpells // these shoudl probably actually be scriptable objects so they are easy to swap in and out and save
{
    [Tooltip("Doesn't do shit but gives some info")]
    public string SpellName;
    public UnityEvent<Transform[]> OnHit; //this is where the spell effector is actuall called
    [Tooltip("An extra unity event for the position of where you did OnHit")]
    public UnityEvent<Transform> OnHitPosition;
    public uint Cost;
    [Tooltip("Size of selection")]
    public float Radius = 2;
    public GameObject spellVFX;
    public float animationDuration, hitboxDelay;
}
