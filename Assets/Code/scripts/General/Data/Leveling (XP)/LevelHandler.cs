using UnityEngine;

public class LevelHandler : MonoBehaviour
{
    [SerializeField]
    LevelManager player_level;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Increase_XP(float xp, int ending_coins)
    {
        player_level.Increase_XP(xp, ending_coins);
    }
}
