using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New AreaEffect", menuName = "Area Effects/Zone Data")]
public class AreaEffectData : ScriptableObject
{
    [Header("Shape")]
    public ZoneShape shape = ZoneShape.Circle;
    public Vector2 boxSize = new Vector2(3f, 2f);
    public float circleRadius = 3f;

    [Header("Targeting")]
    public int maxTargets = 100;    

    [Header("Duration")]
    public float zoneLifespan = 0f; 
    public bool sticky = false;

    [Header("Effects")]
    public StatusEffect[] effects;
    [Tooltip("This is just here because using status effects for damage and stuff like that when it only applies if you are in a zone is weird")]

    public int HealthChangePerTick = 0;
    public float refreshInterval = 0.5f;
    public ZoneApplicationMode applicationMode = ZoneApplicationMode.All;

    [Header("Visuals")]
    public GameObject zoneVisualPrefab;
    public Color gizmoColor = new Color(0.2f, 1f, 0.2f, 0.25f);
}
