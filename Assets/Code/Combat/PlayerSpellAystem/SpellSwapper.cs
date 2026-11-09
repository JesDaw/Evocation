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

    [Tooltip("The scroll wheel must be still for this long (seconds) before the next scroll switches spells again. " +
             "Stops one fast flick from skipping past several spells.")]
    [SerializeField] float scrollGestureGap = 0.15f;
    float _lastScrollTime = float.NegativeInfinity;

    void SwapForward(InputAction.CallbackContext context)
    {
        if (context.control != null && context.control.device is Mouse)
        {
            OnScroll(context);
            return;
        }
        if (!CanSwap(context)) return;
        if (DebugLogs) Debug.Log($"Swap forward");
        Switch(true);
    }

    // SwapSpell1 is a pass-through action on the scroll wheel's up/down axis, so this gets every scroll event.
    // Wheel up = next spell, wheel down = previous. One flick of the wheel = one switch: the first bit of
    // scroll switches straight away, and the rest of that flick is ignored.
    void OnScroll(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<float>();
        if (Mathf.Approximately(scroll, 0f)) return;

        float now = Time.unscaledTime;
        bool newFlick = now - _lastScrollTime > scrollGestureGap;
        _lastScrollTime = now;
        if (!newFlick || !CanSwap(context)) return;

        bool forward = scroll > 0f;
        if (DebugLogs) Debug.Log($"Swap {(forward ? "forward" : "back")} (scroll)");
        Switch(forward);
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