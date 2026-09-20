using UnityEngine;
[CreateAssetMenu(fileName = "New Clan", menuName = "AI Clan")]
public class AIClan : ScriptableObject
{
    public AIClanPhase[] Phases;
}
[System.Serializable]
public class AIClanPhase
{
    public string Name;
    public ScriptableStats[] Characters;
    public AnimationCurve AdvantageActivityCurve;
    public AnimationCurve DisadvantageActivityCurve;   
}