using UnityEngine;

[CreateAssetMenu(fileName = "DayCycle", menuName = "Scriptable Objects/DayCycle")]
public class DayCycle : ScriptableObject
{
    public int maxActions = 3;
    public int actionCounter = maxActions;
    private int dayCounter = 3;
    public void decreaseActionCounter()
    {
        actionCounter--;

        if (actionCounter <= 0)
        {
            dayCounter--;
            actionCounter = maxActions;
        }
    }
}
