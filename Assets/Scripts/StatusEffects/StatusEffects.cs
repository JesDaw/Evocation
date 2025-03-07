using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffect", menuName = "StatusEffect", order = 0)]
public class StatusEffect : ScriptableObject
{
    //tick cannot go lower than 0.1 (see Stats script)
    public int _Damage;
    public float _Tick;
    public float _Length;
}