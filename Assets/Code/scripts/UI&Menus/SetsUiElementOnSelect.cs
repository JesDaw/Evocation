using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdamLenzini.UI
{
    /// <summary>
    /// Place this component on a screen's root GameObject (the same one that has SceneActivity).
    /// Set elementToSelect to the first button keyboard navigation should land on in this screen.
    ///
    /// WIRING (Inspector):
    ///   SceneActivity → OnActivityStart → SetsUiElementOnSelect.JumptToElement
    ///
    /// Remove any JumptToElement calls from source-button OnClick lists — they created a
    /// timing problem because ActivateWithFade activates the screen inside a fade callback,
    /// so the destination button wasn't active yet when JumptToElement fired.
    ///
    /// Wiring to OnActivityStart guarantees the button is already active when we select it
    /// because SceneActivity.StartActivity() calls SetActive(true) BEFORE invoking OnActivityStart.
    /// </summary>
    public class SetsUiElementOnSelect : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private Selectable elementToSelect;

        [Header("Visualization")]
        [SerializeField] private bool showVisualization;
        [SerializeField] private Color navigationColour = Color.cyan;
        [SerializeField] private bool ShowDebugLogs = true;

        // ── Gizmos ────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!showVisualization || elementToSelect == null) return;
            Gizmos.color = navigationColour;
            Gizmos.DrawLine(transform.position, elementToSelect.transform.position);
        }

        // ── Init ──────────────────────────────────────────────────────────────

        private void Reset()
        {
            eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null && ShowDebugLogs)
                Debug.Log("[SetsUiElementOnSelect] No EventSystem found in scene.", this);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Wire this to SceneActivity.OnActivityStart on this screen's root GameObject.
        /// When called, the screen is already active so elementToSelect can be selected immediately.
        /// (Typo preserved so any remaining Inspector wiring doesn't break.)
        /// </summary>
        public void JumptToElement()
        {
            if (elementToSelect == null)
            {
                Debug.LogError("[SetsUiElementOnSelect] elementToSelect is null.", this);
                return;
            }

            var uiButton = elementToSelect.GetComponent<UIButtons>();

            if (uiButton != null && UINavigationManager.Instance != null)
            {
                // The screen is already active here (OnActivityStart fires after SetActive(true)),
                // so the manager can select the button immediately if in keyboard mode.
                UINavigationManager.Instance.RegisterScreenDefault(uiButton);
            }
            else
            {
                // Fallback for Selectables without a UIButtons component.
                if (eventSystem != null)
                    eventSystem.SetSelectedGameObject(elementToSelect.gameObject);
                else if (ShowDebugLogs)
                    Debug.LogWarning("[SetsUiElementOnSelect] No EventSystem referenced.", this);
            }
        }
    }
}