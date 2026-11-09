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
    [Tooltip("Prefab with a ZoneVisual component on its root.")]
    public GameObject zoneVisualPrefab;
    public Color ringColor = new Color(0.2f, 1f, 0.2f, 0.9f);
    public Color fillColor = new Color(0.2f, 1f, 0.2f, 0.25f);
    [Tooltip("One-shot zones (refreshInterval <= 0) apply instantly; this keeps the visual around briefly so it can be seen.")]
    public float oneShotVisualDuration = 0.25f;

    [Header("Editor Gizmo")]
    public Color gizmoColor = new Color(0.2f, 1f, 0.2f, 0.25f);
}