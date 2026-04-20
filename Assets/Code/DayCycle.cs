using UnityEngine;

[CreateAssetMenu(fileName = "DayCycle", menuName = "Scriptable Objects/DayCycle")]
public class DayCycle : ScriptableObject
{
    public int maxActions = 3;
    public int actionCounter = 0;
    public int dayCounter = 0;
    public void IncromentActionCounter()
    {
        actionCounter++;

        if (actionCounter >= maxActions)
        {
            dayCounter++;
            actionCounter = 0;
        }
    }
}
