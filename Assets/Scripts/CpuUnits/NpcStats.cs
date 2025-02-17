using UnityEngine;

[CreateAssetMenu(fileName = "Stats", menuName = "Npc", order = 0)]
public class NpcStats : ScriptableObject
{
    public Sprite _Sprite;
    public int _Health;
    public int _Attack;
    public float _Speed;
    public float _StopDistance;
}
    
