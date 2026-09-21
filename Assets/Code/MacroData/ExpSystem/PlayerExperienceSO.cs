using UnityEngine;

[CreateAssetMenu(fileName = "LevelManager", menuName = "Scriptable Objects/LevelManager")]
public class PlayerExperienceSO : ScriptableObject
{
  public float current_xp = 0;

  public void SpendExp(ScriptableStats scriptableStats, float exp)
  {
    current_xp = scriptableStats.TryLevelUp(exp);
  }
  public void AddExp(float amount)
  {
    current_xp += amount;
  }

  public void SubtractExp(float amount)
  {
    current_xp -= amount;
  }
}
