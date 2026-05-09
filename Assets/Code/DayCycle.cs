using UnityEngine;

[CreateAssetMenu(fileName = "DayCycle", menuName = "Scriptable Objects/DayCycle")]
public class DayCycle : ScriptableObject
{
    [Header("Max actions")]
    public int soulCounter = 5;
    public int maxActions = 3;
    public int actionCounter = 0;
    [Header("Days")]
    public int dayCounter = 0;
    public int monthCounter = 0;
    public int yearCounter = 0;
    public void IncrementActionCounter()
    {
        actionCounter++;

        if (actionCounter >= maxActions)
        {
            IncrementDayCounter();
            actionCounter = 0;
        }
    }

    public void ChangeSoul(int soul)
    {
        soulCounter += soul;
    }

    public void IncrementDayCounter()
    {
        dayCounter++;
        soulCounter--;

        if (dayCounter > 30)
        {
            IncrementMonthCounter();
            dayCounter = 0;
        }
    }

    public void IncrementMonthCounter()
    {
        monthCounter++;

        if (monthCounter > 12)
        {
            IncrementYearCounter();
            monthCounter = 0;
        }
    }

    public void IncrementYearCounter()
    {
        yearCounter++;
    }

}
