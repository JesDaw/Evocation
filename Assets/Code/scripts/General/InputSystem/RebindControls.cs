using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to a GameObject in your Controls scene.
/// For each rebindable action, drag the button and the "current control" label into the Inspector.
/// The script handles the Minecraft-style click-to-rebind flow automatically.
/// </summary>
public class RebindControls : MonoBehaviour
{
    [System.Serializable]
    public class RebindEntry
    {
        [Tooltip("The action map name, e.g. 'Player', 'MagicController'")]
        public string actionMapName;

        [Tooltip("The action name, e.g. 'Attack', 'CastSpell'")]
        public string actionName;

        [Tooltip("Which binding index to rebind (0 = first binding for that action)")]
        public int bindingIndex = 0;

        [Tooltip("The button the player clicks to start rebinding this action")]
        public Button rebindButton;

        [Tooltip("The label that shows the current key binding (the 'A' labels in your screenshot)")]
        public TextMeshProUGUI currentBindingLabel;
    }

    [Header("Rebind Entries")]
    [SerializeField] private RebindEntry[] rebindEntries;

    [Header("UI Feedback")]
    [SerializeField] private GameObject listeningOverlay;   // Optional: dim overlay while waiting for input
    [SerializeField] private TextMeshProUGUI listeningLabel; // Optional: "Press a key..." text

    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
    private RebindEntry _currentEntry;
    private bool _isRebinding;

    // ========================= Lifecycle =========================

    void OnEnable()
    {
        RefreshAllLabels();
        RegisterAllButtons();
    }

    void OnDisable()
    {
        UnregisterAllButtons();
        CancelRebind(); // Safety: clean up if scene unloads mid-rebind
    }

    // ========================= Setup =========================

    void RegisterAllButtons()
    {
        foreach (var entry in rebindEntries)
        {
            if (entry.rebindButton == null) continue;
            var captured = entry; // Capture for lambda
            entry.rebindButton.onClick.AddListener(() => StartRebind(captured));
        }
    }

    void UnregisterAllButtons()
    {
        foreach (var entry in rebindEntries)
        {
            if (entry.rebindButton == null) continue;
            entry.rebindButton.onClick.RemoveAllListeners();
        }
    }

    // ========================= Label Refresh =========================

    void RefreshAllLabels()
    {
        foreach (var entry in rebindEntries)
            RefreshLabel(entry);
    }

    void RefreshLabel(RebindEntry entry)
    {
        if (entry.currentBindingLabel == null) return;

        var action = GetAction(entry);
        if (action == null)
        {
            entry.currentBindingLabel.text = "???";
            return;
        }

        // GetBindingDisplayString handles overrides automatically
        entry.currentBindingLabel.text = action.GetBindingDisplayString(
            entry.bindingIndex,
            InputBinding.DisplayStringOptions.DontIncludeInteractions
        );
    }

    // ========================= Rebinding =========================

    void StartRebind(RebindEntry entry)
    {
        if (_isRebinding) return;

        var action = GetAction(entry);
        if (action == null)
        {
            Debug.LogWarning($"RebindControls: Could not find action '{entry.actionName}' in map '{entry.actionMapName}'");
            return;
        }

        _isRebinding = true;
        _currentEntry = entry;

        

        // Update UI to show "waiting" state
        if (entry.currentBindingLabel != null)
            entry.currentBindingLabel.text = "...";

        SetButtonsInteractable(false);
        ShowListeningOverlay(true);

        action.actionMap.Disable();

        _rebindOperation = action
            .PerformInteractiveRebinding(entry.bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .WithControlsExcluding("<Mouse>/leftButton")
            .OnMatchWaitForAnother(0.1f)
            .OnPotentialMatch(op =>
                {
                    Debug.Log("Potential match: " + op.selectedControl);
                })
            .OnComplete(_ => OnRebindComplete())
            .OnCancel(_ => OnRebindCancelled());

        _rebindOperation.Start();
    }

    void OnRebindComplete()
    {
        CleanupOperation();
        RefreshLabel(_currentEntry);
        SaveRebinds();
        FinishRebind();
    }

    void OnRebindCancelled()
    {
        CleanupOperation();
        RefreshLabel(_currentEntry); // Restore the original label
        FinishRebind();
    }

    void CancelRebind()
    {
        if (!_isRebinding) return;
        _rebindOperation?.Cancel();
    }

    void FinishRebind()
    {
        _isRebinding = false;

        // Re-enable whichever map we disabled before rebinding
        if (_currentEntry != null)
        {
            var action = GetAction(_currentEntry);
            action?.actionMap.Enable();
        }

        _currentEntry = null;

        SetButtonsInteractable(true);
        ShowListeningOverlay(false);

        // Now restore your actual intended input mode
        GlobalInputManager.Instance.SetPauseMenuMode();
    }

    // ========================= Reset =========================

    /// <summary>
    /// Call this from a "Reset All" button's onClick if you want one.
    /// </summary>
    public void ResetAllBindings()
    {
        var asset = GlobalInputManager.Instance.InputActions.asset;
        asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey("rebinds");
        PlayerPrefs.Save();
        RefreshAllLabels();
    }

    /// <summary>
    /// Reset a single entry. Wire this to individual reset buttons if desired.
    /// </summary>
    public void ResetBinding(int entryIndex)
    {
        if (entryIndex < 0 || entryIndex >= rebindEntries.Length) return;
        var entry = rebindEntries[entryIndex];
        var action = GetAction(entry);
        action?.RemoveBindingOverride(entry.bindingIndex);
        RefreshLabel(entry);
        SaveRebinds();
    }

    // ========================= Helpers =========================

    InputAction GetAction(RebindEntry entry)
    {
        var asset = GlobalInputManager.Instance.InputActions.asset;
        var map = asset.FindActionMap(entry.actionMapName, throwIfNotFound: false);
        return map?.FindAction(entry.actionName, throwIfNotFound: false);
    }

    void SaveRebinds()
    {
        var json = GlobalInputManager.Instance.InputActions.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", json);
        PlayerPrefs.Save();
    }

    void SetButtonsInteractable(bool interactable)
    {
        foreach (var entry in rebindEntries)
        {
            if (entry.rebindButton != null)
                entry.rebindButton.interactable = interactable;
        }
    }

    void ShowListeningOverlay(bool show)
    {
        if (listeningOverlay != null)
            listeningOverlay.SetActive(show);
    }

    void CleanupOperation()
    {
        _rebindOperation?.Dispose();
        _rebindOperation = null;
    }

    void OnDestroy()
    {
        _rebindOperation?.Cancel();
        CleanupOperation();
    }
}