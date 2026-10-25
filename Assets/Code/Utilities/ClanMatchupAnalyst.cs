using UnityEngine;

// Intentionally different from the anchor-based cost system: this compares
// ClanA's actual roster against ClanB's actual roster (and vice versa) for
// matchup/RPS exploration, not for pricing. Using each clan's own live
// average here is correct for "does clan A beat clan B on average" — it's
// only the cost-calibration system (MasterBalancingScript/ClanStats) that
// needed a fixed reference instead of a moving one. Flag if you'd rather
// this also use a fixed anchor for some other reason.
[ExecuteInEditMode]
public class ClanMatchupAnalyst : MonoBehaviour
{
    public ClanStats ClanA;
    public ClanStats ClanB;

    [Header("Results: Clan A vs Clan B")]
    public float ClanA_Advantage;
    [TextArea(3, 5)] public string Analysis_A;

    [Header("Results: Clan B vs Clan A")]
    public float ClanB_Advantage;
    [TextArea(3, 5)] public string Analysis_B;

    [Header("Calibration")]
    [Tooltip("Right-click this component and choose 'Recalibrate Local Offset' after changing either " +
             "clan's roster — no longer recomputed every frame, same reasoning as MasterBalancingScript.")]
    public float LocalMinOffset;
}