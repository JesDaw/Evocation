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
        totalMana.text = "Mana: " + _totalMana.ToString();
        ManaBarImage.value = (float)_totalMana / 100;
    }


}