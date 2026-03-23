using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PlayerSpells 
{
    [Tooltip("Doesn't do shit but gives some info")]
    public string SpellName;
    public UnityEvent<Transform[]> OnHit;
    [Tooltip("An extra unity event for the position of where you did OnHit")]
    public UnityEvent<Transform> OnHitPosition;
    public uint Cost;
    [Tooltip("Size of selection")]
    public float Radius = 2;
}
