using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ManaSystem))]
public class SpellsManager : MonoBehaviour
{
    public List<PlayerSpells> playerSpells = new List<PlayerSpells>();
    ManaSystem manaSystem;


    void Awake() =>
        manaSystem = GetComponent<ManaSystem>();

    void Start()
    {
        manaSystem.SpendMana(playerSpells[0].Cost);
        manaSystem.SpendMana(playerSpells[1].Cost);
    }
}
