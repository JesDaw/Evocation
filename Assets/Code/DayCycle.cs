using UnityEngine;

[CreateAssetMenu(fileName = "DayCycle", menuName = "Scriptable Objects/DayCycle")]
public class DayCycle : ScriptableObject
{
    public int maxActions = 3;
    public int actionCounter = 0;
    public int soulCounter = 5;
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

    public void IncreaseSoul(int soul)
    {
        soulCounter += soul;
    }

    public int GetSoul()
    {
        return soulCounter;
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

    public int GetDay()
    {
        return dayCounter;
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

    public int GetMonth()
    {
        return monthCounter;
    }

    public void IncrementYearCounter()
    {
        yearCounter++;
    }

    public int GetYear()
    {
        return yearCounter;
    }
}
