using UnityEngine;

public class LevelExpManager : MonoBehaviour
{
    [SerializeField] PlayerExperienceSO playerExperienceSO;
    public int EndingCoinsThreashhold = 200;
    public int MaxTimeRemaining = 600;
    [SerializeField] float LevelExp = 10f;
    public void ChangeXP(int ending_coins, int timeRemaining)
    {
        LevelExp *= ExpMultiplier(ending_coins, EndingCoinsThreashhold);
        LevelExp *= ExpMultiplier(timeRemaining, MaxTimeRemaining);
        playerExperienceSO.AddExp(LevelExp);
    }

    int ExpMultiplier(int score, int threashhold)
    {
        return 1 + (score / threashhold);
    }
}
