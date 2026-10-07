using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the spell list and which one is selected. Swapping is blocked while
/// SpellCaster is aiming or mid-cast.
/// </summary>
public class SpellSwapper : MonoBehaviour
{
    public static SpellSwapper Instance { get; private set; }

    public List<SpellDefinition> spells = new List<SpellDefinition>();
    public UnityEvent OnSwapSpells;

    [SerializeField] int currentIndex = 0;
    [SerializeField] bool DebugLogs;

    public SpellDefinition CurrentSpell;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SubscribeToInputs();
        if (DebugLogs) Debug.Log($"spells.Count = {spells.Count}");
        if (spells.Count == 0)
            return;

        currentIndex = Mathf.Clamp(currentIndex, 0, spells.Count - 1);
        CurrentSpell = spells[currentIndex];

        UpdateMagicUIFunctions.Instance.UpdateSpellGUI(CurrentSpell);
        OnSwapSpells.Invoke();
    }

    void SubscribeToInputs()
    {
        var input = GlobalInputManager.Instance.InputActions.MagicController;

        input.SwapSpell1.performed += SwapForward;
        input.SwapSpell2.performed += SwapBackward;
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null)
            return;

        var input = GlobalInputManager.Instance.InputActions.MagicController;

        input.SwapSpell1.performed -= SwapForward;
        input.SwapSpell2.performed -= SwapBackward;
    }

    void SwapForward(InputAction.CallbackContext context)
    {
        if (DebugLogs) Debug.Log($"Swap forward");
        Switch(true);
    }

    void SwapBackward(InputAction.CallbackContext context)
    {
        if (DebugLogs) Debug.Log($"Swap Back");
        Switch(false);
    }

    void Switch(bool forward)
    {
        if (DebugLogs) Debug.Log($"spells.Count = {spells.Count}");
        if (spells.Count == 0) return;


        if (SpellCaster.Instance == null) 
        {
            Debug.LogWarning($"SpellCaster.Instance != null)");
            return;
        }

        int len = spells.Count;

        currentIndex = (currentIndex + (forward ? 1 : -1) + len) % len;

        CurrentSpell = spells[currentIndex];

        UpdateMagicUIFunctions.Instance.UpdateSpellGUI(CurrentSpell);
        OnSwapSpells.Invoke();
        if (DebugLogs) Debug.Log($"Event invoked");
    }

    void OnDestroy()
    {
        UnsubscribeFromInputs();
    }
}