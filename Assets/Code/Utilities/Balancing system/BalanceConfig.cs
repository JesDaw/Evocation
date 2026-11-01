using UnityEngine;

[CreateAssetMenu(fileName = "BalanceConfig", menuName = "Balance/Config", order = 1)]
public class BalanceConfig : ScriptableObject
{
    public ScriptableStats anchorUnit;
    public float anchorCost = 50f;

    // Sensible defaults so power is never 0 even before running the simulation!
    public BalanceSimulationRunner.CobbDouglasElasticities elasticities = new BalanceSimulationRunner.CobbDouglasElasticities
    {
        scaleConstant = 1.0f,
        e_Health      = 0.8f,
        e_DPS         = 1.0f,
        e_Range       = 0.5f,
        e_MoveSpeed   = 0.3f,
        e_KBD         = 0.2f,
        e_KBH         = 0.2f,
        rSquared      = 1.0f
    };
}