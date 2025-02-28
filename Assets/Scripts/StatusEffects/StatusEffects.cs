using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "StatusEffect", menuName = "StatusEffect", order = 0)]
public class StatusEffect : ScriptableObject
{
    public float Damage;
    public float Tick;
    public float Length;
    public UnityEvent OnTick;
}