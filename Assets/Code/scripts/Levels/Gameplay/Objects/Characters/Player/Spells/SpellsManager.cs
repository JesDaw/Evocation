using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(ManaSystem))]
public class SpellsManager : MonoBehaviour
{
    public List<PlayerSpells> playerSpells = new List<PlayerSpells>();
    ManaSystem manaSystem;
    Transform[] currentSelected = new Transform[0];
    [SerializeField] uint currentSpellContext = 0;
    [SerializeField] Transform detectionRadiusObject;
    [SerializeField] bool DebugLogs = false;
    //this means spell is ready and primed (can't be switched)
    bool charged = false;

    void Awake() =>
        manaSystem = GetComponent<ManaSystem>();

    void InvokeSpell()
    {
        if(charged) return;
        charged = true;

        detectionRadiusObject.localPosition = new Vector3(0, 0, 0);
        detectionRadiusObject.gameObject.SetActive(true);

        float radius = playerSpells[(int)currentSpellContext].Radius;
        detectionRadiusObject.localScale = new Vector3(radius, radius, 0);

        if(DebugLogs) Debug.Log("Spells Invoked");
        if(manaSystem.SpendMana(playerSpells[(int)currentSpellContext].Cost))
            playerSpells[(int)currentSpellContext].OnHit.Invoke(currentSelected);
    }

    void SwitchSpells(bool _forward)
    {
        if(charged) return;
        int len = playerSpells.Count;
        currentSpellContext = (uint)((currentSpellContext + (_forward ? 1 : len - 1)) % len);
    }

    void Start()
    {
        detectionRadiusObject.gameObject.SetActive(false);
        SubscribeToSpells();
    }

    Vector2 magicRadiusHoverInput;

    void Update()
    {
        var input = GlobalInputManager.Instance.InputActions.MagicController;
        if(!input.enabled) return;

        magicRadiusHoverInput = input.Look.ReadValue<Vector2>() * 0.02f;

        detectionRadiusObject.position +=
            new Vector3(magicRadiusHoverInput.x, magicRadiusHoverInput.y, 0);
    }

    void SubscribeToSpells()
    {
        var input = GlobalInputManager.Instance.InputActions.MagicController;

        input.CastSpell.performed += _ => InvokeSpell();
        input.SwapSpell1.performed += _ => SwitchSpells(true);
        input.SwapSpell2.performed += _ => SwitchSpells(false);
    }

    void UnsubscribeToSpells()
    {
        //not sure if this would work...
        //ehh might as well
        var input = GlobalInputManager.Instance.InputActions.MagicController;

        input.CastSpell.performed -= _ => InvokeSpell();
        input.SwapSpell1.performed -= _ => SwitchSpells(true);
        input.SwapSpell2.performed -= _ => SwitchSpells(false);
    }

    private Vector3 GetTargetWorldPosition(Vector2 _screenPos)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(_screenPos.x, _screenPos.y, Camera.main.nearClipPlane));
        worldPos.z = 0;
        return worldPos;
    }

    void OnDestroy()
    {
        UnsubscribeToSpells();
    }
}
