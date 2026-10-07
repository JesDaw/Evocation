using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Heal", fileName = "New Heal Spell")]
public class HealSpell : SpellDefinition
{

    
    void OnValidate() 
    {
        castMode = SpellCastMode.SelfCast; 
    }
}