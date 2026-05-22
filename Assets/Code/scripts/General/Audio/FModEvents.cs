using UnityEngine;
using FMODUnity;

public class FModEvents : MonoBehaviour
{
    [field: Header("UI SFX")]
    [field: SerializeField] public EventReference menuClick { get; private set; }
    [field: SerializeField] public EventReference dialogueType { get; private set; }
    [field: SerializeField] public EventReference back { get; private set; }

    [field: Header("Character Select")]
    [field: SerializeField] public EventReference showCharacterInfo { get; private set; }
    [field: SerializeField] public EventReference addCharacterToParty { get; private set; }
    [field: SerializeField] public EventReference removeCharacterFromParty { get; private set; }
    [field: SerializeField] public EventReference openCharacterSelect { get; private set; }
    [field: SerializeField] public EventReference closeCharacterSelect { get; private set; }
    [field: Header("Characters")]
    [field: SerializeField] public EventReference HoodedGuy { get; private set; }
    [field: SerializeField] public EventReference WolfRider { get; private set; }
    [field: SerializeField] public EventReference WolfRunner { get; private set; }
    [field: SerializeField] public EventReference WolfHammer { get; private set; }
    [field: SerializeField] public EventReference WolfMage { get; private set; }

    [field: Header("Start Battle")]
    [field: SerializeField] public EventReference engageInBattle { get; private set; }
    [field: SerializeField] public EventReference backToScouting { get; private set; }

    [field: Header("Pause")]
    [field: SerializeField] public EventReference pauseGame { get; private set; }
    [field: SerializeField] public EventReference unpauseGame { get; private set; }

    [field: Header("Gameplay SFX")]
    [field: SerializeField] public EventReference spawnTroop { get; private set; }
    [field: SerializeField] public EventReference claimLocation { get; private set; }
    
    //footsteps
    [field: SerializeField] public EventReference walkWood { get; private set; }
    [field: SerializeField] public EventReference walkstone { get; private set; }


    [field: Header("Melee Combat SFX")]    
    [field: SerializeField] public EventReference attack { get; private set; }
    [field: SerializeField] public EventReference takeDamage { get; private set; }
    [field: SerializeField] public EventReference knockback { get; private set; }
    [field: SerializeField] public EventReference die { get; private set; }
    [field: Header("projectile Combat SFX")]
    [field: SerializeField] public EventReference shootFireball { get; private set; }
    [field: SerializeField] public EventReference fireballHit { get; private set; }

    [field: Header("Ambiance")]
    [field: SerializeField] public EventReference ambiance { get; private set; }

    [field: Header("Music")]
    [field: SerializeField] public EventReference music { get; private set; }

    
    public static FModEvents instance { get; private set; }

    void Awake()
    {
        if (instance != null && instance != this) 
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }
}