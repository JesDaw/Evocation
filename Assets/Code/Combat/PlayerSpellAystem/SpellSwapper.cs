using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

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
    [SerializeField] TextMeshProUGUI totalMana;
    [SerializeField] TextMeshProUGUI cost;
    [SerializeField] Slider ManaBarImage;
    [SerializeField] Image spellIconHolder; 
    [SerializeField] bool DebugLogs;

    public SpellDefinition CurrentSpell;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SubscribeToInputs();
        if (ManaSystem.Instance != null && totalMana != null && ManaBarImage != null)
            UpdateTotalMana(ManaSystem.Instance.PlayerMana);
        if (DebugLogs) Debug.Log($"spells.Count = {spells.Count}");
        if (spells.Count == 0)
            return;

        currentIndex = Mathf.Clamp(currentIndex, 0, spells.Count - 1);
        CurrentSpell = spells[currentIndex];
        UpdateSpellGUI();
        OnSwapSpells.Invoke();
    }

    void SubscribeToInputs()
    {
        var input = GlobalInputManager.Instance.InputActions.MagicController;

        input.SwapSpell1.performed += SwapForward;
        input.SwapSpell2.performed += SwapBackward;
    }
    void OnDestroy()
    {
        UnsubscribeFromInputs();
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null)
            return;

        var input = GlobalInputManager.Instance.InputActions.MagicController;

        input.SwapSpell1.performed -= SwapForward;
        input.SwapSpell2.performed -= SwapBackward;
    }

    #region switching

    void SwapForward(InputAction.CallbackContext context)
    {
        if (!CanSwap(context)) return;
        if (DebugLogs) Debug.Log($"Swap forward");
        Switch(true);
    }

    void SwapBackward(InputAction.CallbackContext context)
    {
        if (!CanSwap(context)) return;
        if (DebugLogs) Debug.Log($"Swap Back");
        Switch(false);
    }

    static bool CanSwap(InputAction.CallbackContext context)
    {
        // No swapping mid-aim or mid-cast: the aimed spell's radius and cost are already locked in.
        if (SpellCaster.Instance != null && SpellCaster.Instance.IsBusy) return false;

        // In free cam the mouse wheel zooms, so it shouldn't also change spells.
        bool fromMouse = context.control != null && context.control.device is Mouse;
        if (fromMouse && GlobalInputManager.Instance != null && GlobalInputManager.Instance.CurrentMode == InputMode.FreeCam)
            return false;

        return true;
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

        UpdateSpellGUI();
        OnSwapSpells.Invoke();
        if (DebugLogs) Debug.Log($"Event invoked");
    }
    #endregion

    void UpdateSpellGUI()
    {
        cost.text = CurrentSpell.Cost.ToString();
        spellIconHolder.sprite = CurrentSpell.Icon;
    }

    public void UpdateTotalMana(int _totalMana)
    {
        totalMana.text = _totalMana.ToString();
        ManaBarImage.value = (float)_totalMana / 100;
    }


}