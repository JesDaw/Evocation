using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(ManaSystem))]
public class SpellsManager : MonoBehaviour
{
    public List<PlayerSpells> PlayerSpells = new List<PlayerSpells>();
    ManaSystem manaSystem;
    public List<Transform> CurrentlySelected = new List<Transform>();
    public UnityEvent<PlayerSpells> OnSwapSpells;
    [SerializeField] uint currentSpellContext = 0;
    [SerializeField] Transform detectionRadiusObject;
    [SerializeField] Transform playerPos;
    [SerializeField] bool DebugLogs = false;
    //this means spell is ready and primed (can't be switched)
    bool charged = false;

    void Awake() =>
        manaSystem = GetComponent<ManaSystem>();

    void InvokeSpell()
    {
        if(charged)
        {
            UseSpell();
            return;
        }

        if(!manaSystem.SpendMana(PlayerSpells[(int)currentSpellContext].Cost)) return;
        charged = true;

        GlobalInputManager.Instance.DisableCursor();
        detectionRadiusObject.position = playerPos.position;
        detectionRadiusObject.gameObject.SetActive(true);

        float radius = PlayerSpells[(int)currentSpellContext].Radius;
        detectionRadiusObject.localScale = new Vector3(radius, radius, 0);
        if(DebugLogs) Debug.Log("Spells Invoked");
    }

    void UseSpell()
    {
        PlayerSpells[(int)currentSpellContext].OnHit.Invoke(CurrentlySelected.ToArray());
        PlayerSpells[(int)currentSpellContext].OnHitPosition.Invoke(detectionRadiusObject);
        if(DebugLogs) Debug.Log("Spells Used");
        detectionRadiusObject.gameObject.SetActive(false);
        GlobalInputManager.Instance.EnableCursor();
        charged = false;
    }

    void SwitchSpells(bool _forward)
    {
        if(charged) return;
        int len = PlayerSpells.Count;
        currentSpellContext = (uint)((currentSpellContext + (_forward ? 1 : len - 1)) % len);
        OnSwapSpells.Invoke(PlayerSpells[(int)currentSpellContext]);
    }

    void Start()
    {
        detectionRadiusObject.gameObject.SetActive(false);
        SubscribeToSpells();

        OnSwapSpells.Invoke(PlayerSpells[(int)currentSpellContext]); // index error here
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
