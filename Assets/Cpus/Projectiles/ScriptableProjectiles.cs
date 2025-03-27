using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "Projectiles", menuName = "Projectiles", order = 0)]
public class ScrProjectiles : ScriptableObject
{
    public Sprite _Appearance;
    public AnimationCurve _TrajectoryCurve;
    public int _Damage;
    public float _Speed;
}