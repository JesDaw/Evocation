using UnityEngine;
using UnityEngine.Events;

public class StructureReseter : MonoBehaviour
{
    [SerializeField] bool active;
    [SerializeField] FloatVariable BuildingHealth;
    [SerializeField] UnityEvent OnClaim;

    void Start()
    {
        active = false;
        BuildingHealth.Reset();
    }

    void ClaimBuilding(bool PlayerTeam)
    {
        active = true;
        OnClaim.Invoke();
        BuildingHealth.Reset();
    }
}
