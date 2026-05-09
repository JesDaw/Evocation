using UnityEngine;

public class HubWorldExpManager : MonoBehaviour
{
    [SerializeField] PlayerExperienceSO playerExperienceSO;
    public void TryLevelUp(ScriptableStats scriptableStats)
    {
        scriptableStats.TryLevelUp(playerExperienceSO.current_xp);
    }
}

