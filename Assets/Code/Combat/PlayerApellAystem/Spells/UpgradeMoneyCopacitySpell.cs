using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Money", fileName = "Money Copacity Upgrade Spell")]
public class UpgradeMoneyCopacitySpell : SpellDefinition
{
    protected override void OtherEffects()
    {
        Money.Instance.UpgradeMaxMoney();
    }
}
