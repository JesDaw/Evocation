using UnityEngine;

[CreateAssetMenu(fileName = "LevelManager", menuName = "Scriptable Objects/LevelManager")]
public class LevelManager : ScriptableObject
{
  public int level = 0;
  public float xp_to_next_lvl = 100;
  public float current_xp = 0;
  public float lvl_scaling_percent = 0.5f;

  public void Increase_XP(float xp, float multiplier)
  {
    current_xp += xp * multiplier;
    if (current_xp >= xp_to_next_lvl) {
      LevelUp();
    }
  }

  private float CalculateCoinMultiplier(int ending_coins)
  {
    return 1 + (ending_coins / 200);
  }

  private void LevelUp()
  {
    level += 1;
    xp_to_next_lvl += xp_to_next_lvl * lvl_scaling_percent;
    current_xp = 0;
  }

}
