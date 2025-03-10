using UnityEngine;


public class MoneyGenBuilding : MonoBehaviour
{
    [SerializeField] FloatVariable genPerSec;
    [SerializeField] FloatVariable moneyAmount;
    [SerializeField] bool active;
    [SerializeField] float ActivationMultiplier;
    [SerializeField] FloatVariable BuildingHealth;

    void OnEnable()
    {
        active = false;
        ActivationMultiplier = 1f; 
        BuildingHealth.Reset();
    }

    void ClaimBuilding(bool PlayerTeam)
    {
        active = true;
        BuildingBonus(PlayerTeam);
        BuildingHealth.Reset();
    }

    void BuildingBonus(bool PlayerTeam)
    {
        if(PlayerTeam)
        {
            genPerSec._Value *= ActivationMultiplier;
        }
        else
        {
            //apply effect on other team
        }
    }



}
