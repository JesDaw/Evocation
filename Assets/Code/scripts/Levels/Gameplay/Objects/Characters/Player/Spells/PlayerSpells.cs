using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PlayerSpells 
{
    [Tooltip("Doesn't do shit but gives some info")]
    public string SpellName;
    public UnityEvent<Transform[]> OnHit;
    public uint Cost;
    [Tooltip("Size of selection")]
    public float Radius = 2;
}
