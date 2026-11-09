using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shows one control hint at a time near the bottom of the screen.
/// A hint fades in once its controls are usable, and fades out for good the first time the player uses one of them.
/// Hints whose controls were already used before they came up are skipped.
///
/// Edit the hint list in the inspector. Messages are TextMeshPro rich text, so key icons from the
/// default sprite asset work, e.g. <sprite name="button_prompts_e">.
/// </summary>
public class TutorialHints : MonoBehaviour
{
    public enum ShowWhen
    {
        ControlsAvailable,   // whenever any of the hint's actions is enabled
        InFreeCam,
        AimingSpell,
        HasMultipleHeroes,
        CanAffordSpell,      // enough mana for the currently selected spell
    }

    [Serializable]
    public class Hint
    {
        [TextArea(1, 3)] public string Message;
        [Tooltip("Input actions as \"Map/Action\", e.g. Player/Move. Using any one of them completes the hint.")]
        public string[] Actions;
        public ShowWhen Condition = ShowWhen.ControlsAvailable;
        [Tooltip("Only show after the hint above this one in the list has been completed.")]
        public bool AfterPreviousHint;
        [Tooltip("Only complete when the control is used while this hint is on screen, so an accidental " +
                 "press/scroll before the hint appears doesn't skip it.")]
        public bool OnlyCountWhileShown;

        public Hint() { }
        public Hint(string message, ShowWhen condition, params string[] actions)
        {
            Message = message;
            Condition = condition;
            Actions = actions;
        }

        [NonSerialized] public bool Done;
        [NonSerialized] public bool ConditionMetLastFrame;
        [NonSerialized] public List<InputAction> ResolvedActions = new List<InputAction>();
    }

    [Tooltip("On: always use the hint list written in TutorialHints.cs (DefaultHints). Off: use the list below, edited in the inspector.")]
    [SerializeField] bool useHintsFromCode = true;
    [SerializeField] List<Hint> hints = DefaultHints();

    [Tooltip("Don't show hints (or count key presses) until the level reaches its Combat state, i.e. after scouting/loadout.")]
    [SerializeField] bool waitForCombat = true;

    [Header("Timing (real-time seconds)")]
    [SerializeField] float startDelay = 1.5f;
    [SerializeField] float fadeDuration = 0.4f;
    [Tooltip("Pause before the next hint when one was hidden (dialogue, pause...). After the player uses a hint's key, the next one comes up right away.")]
    [SerializeField] float gapBetweenHints = 0.75f;
    const float CompletedFadeOutTime = 0.15f;

    [Header("Look (only used when no custom UI is assigned)")]
    [SerializeField] float textSize = 26f;
    [SerializeField] float distanceFromBottom = 150f;
    [SerializeField] Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    [Tooltip("Corner roundness of the background, in pixels at 1920x1080.")]
    [SerializeField] int cornerRadius = 18;

    [Header("Optional custom UI")]
    [Tooltip("Assign both to use your own UI instead of the auto-built one.")]
    [SerializeField] TMP_Text customLabel;
    [SerializeField] CanvasGroup customGroup;

    [SerializeField] bool DebugLogs = false;

    TMP_Text _label;
    CanvasGroup _group;
    readonly List<(InputAction action, Action<InputAction.CallbackContext> handler)> _subscriptions = new();

    static List<Hint> DefaultHints() => new List<Hint>
    {
        new Hint("<sprite name=\"button_prompts_a\"> and <sprite name=\"button_prompts_d\"> to move",
            ShowWhen.ControlsAvailable, "Player/Move"),
        new Hint("<b><u>Left Click</u></b> to attack",
            ShowWhen.ControlsAvailable, "Player/Attack"),
        new Hint("<sprite name=\"button_prompts_1\"> <voffset=0.15em>-</voffset> <sprite name=\"button_prompts_5\"> to spawn allies",
            ShowWhen.ControlsAvailable, "SpawnerController/Spawn1", "SpawnerController/Spawn2",
            "SpawnerController/Spawn3", "SpawnerController/Spawn4", "SpawnerController/Spawn5"),
        new Hint("<b><u>F</u></b> to buy an extra life",
            ShowWhen.ControlsAvailable, "SpawnerController/SpawnPlayer"),
        new Hint("<b><u>V</u></b> for free cam mode",
            ShowWhen.ControlsAvailable, "ControlManager/ToggleCameraControl"),
        new Hint("<sprite name=\"button_prompts_w\"><sprite name=\"button_prompts_a\"><sprite name=\"button_prompts_s\"><sprite name=\"button_prompts_d\"> to move the camera, <b><u>Mouse Wheel</u></b> to zoom",
            ShowWhen.InFreeCam, "Camera/Move", "Camera/Zoom"),
        new Hint("<b><u>V</u></b> to exit free cam mode",
            ShowWhen.InFreeCam, "ControlManager/ToggleCameraControl"),
        // Spell hints only count while on screen: spells are the least obvious controls, so don't let a stray input skip them.
        new Hint("Hold <b><u>Right Click</u></b> to use a spell",
            ShowWhen.CanAffordSpell, "MagicController/AimSpell") { OnlyCountWhileShown = true },
        new Hint("<sprite name=\"button_prompts_q\"> or <b><u>Mouse Wheel Up</u></b> to switch spells",
            ShowWhen.ControlsAvailable, "MagicController/SwapSpell1", "MagicController/SwapSpell2") { AfterPreviousHint = true, OnlyCountWhileShown = true },
        new Hint("<b><u>Left Click</u></b> to cast, let go of <b><u>Right Click</u></b> to cancel",
            ShowWhen.AimingSpell, "MagicController/CastSpell") { OnlyCountWhileShown = true },
        new Hint("<b><u>Z</u></b> and <b><u>X</u></b> to cycle through heroes",
            ShowWhen.HasMultipleHeroes, "ControlManager/PreviousPlayer", "ControlManager/NextPlayer"),
    };

    #region setup
    void Awake()
    {
        if (useHintsFromCode) hints = DefaultHints();
        if (customLabel != null && customGroup != null)
        {
            _label = customLabel;
            _group = customGroup;
        }
        else
        {
            BuildUI();
        }
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;
    }

    void Start() => StartCoroutine(Run());

    void OnDestroy()
    {
        foreach (var (action, handler) in _subscriptions)
            action.performed -= handler;
        _subscriptions.Clear();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("Tutorial Hints Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _group = canvasGO.GetComponent<CanvasGroup>();

        var panelGO = new GameObject("Hint Panel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        panelGO.transform.SetParent(canvasGO.transform, false);

        var panelRect = (RectTransform)panelGO.transform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, distanceFromBottom);

        var image = panelGO.GetComponent<Image>();
        image.sprite = CreateRoundedSprite(cornerRadius);
        image.type = Image.Type.Sliced;
        image.color = backgroundColor;
        image.raycastTarget = false;

        var layout = panelGO.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 14, 14);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        var fitter = panelGO.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textGO = new GameObject("Hint Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(panelGO.transform, false);

        var text = textGO.GetComponent<TextMeshProUGUI>();
        text.fontSize = textSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        _label = text;
    }

    /// <summary>White rounded rectangle, 9-sliced so the corners stay round at any panel size.</summary>
    static Sprite CreateRoundedSprite(int radius)
    {
        radius = Mathf.Max(1, radius);
        int size = radius * 2 + 2;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Distance from the nearest corner circle's centre (0 inside the straight middle strip).
                float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius));
                float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius));
                float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    void Subscribe()
    {
        var asset = GlobalInputManager.Instance.InputActions.asset;
        foreach (var hint in hints)
        {
            hint.ResolvedActions.Clear();
            if (hint.Actions == null) continue;

            foreach (var actionName in hint.Actions)
            {
                var action = asset.FindAction(actionName, throwIfNotFound: false);
                if (action == null)
                {
                    Debug.LogWarning($"[TutorialHints] No input action called \"{actionName}\" (hint: {hint.Message})");
                    continue;
                }
                hint.ResolvedActions.Add(action);

                var thisHint = hint;
                Action<InputAction.CallbackContext> handler = _ => OnHintActionUsed(thisHint);
                action.performed += handler;
                _subscriptions.Add((action, handler));
            }
        }
    }
    #endregion

    #region logic
    void OnHintActionUsed(Hint hint)
    {
        // Uses last frame's state: the same press may flip the mode this frame (e.g. V leaving free cam).
        if (hint.Done || !hint.ConditionMetLastFrame) return;
        // "On screen" = this hint is the one being shown and is at least half faded in.
        if (hint.OnlyCountWhileShown && (_shownHint != hint || _group.alpha < 0.5f)) return;
        hint.Done = true;
        if (DebugLogs) Debug.Log($"[TutorialHints] Completed: {hint.Message}");
    }

    void LateUpdate()
    {
        if (GlobalInputManager.Instance == null) return;
        if (!_combatStarted && CombatHasStarted()) _combatStarted = true;
        foreach (var hint in hints)
            hint.ConditionMetLastFrame = _combatStarted && ConditionMet(hint);
    }

    bool _combatStarted;
    Hint _shownHint;

    bool CombatHasStarted()
    {
        if (!waitForCombat || LevelStateManager.Instance == null) return true;
        return LevelStateManager.Instance.CurrentState is CombatState;
    }

    IEnumerator Run()
    {
        while (GlobalInputManager.Instance == null) yield return null;
        Subscribe();

        while (!_combatStarted) yield return null;
        yield return new WaitForSecondsRealtime(startDelay);

        while (true)
        {
            Hint next = NextShowableHint();
            if (next == null)
            {
                if (hints.TrueForAll(h => h.Done || h.ResolvedActions.Count == 0)) break;
                yield return null;
                continue;
            }

            yield return Show(next);
            if (!next.Done) yield return new WaitForSecondsRealtime(gapBetweenHints);
        }

        if (DebugLogs) Debug.Log("[TutorialHints] All hints completed");
    }

    IEnumerator Show(Hint hint)
    {
        _label.text = hint.Message;
        _shownHint = hint;

        while (true)
        {
            bool canShow = CanShow(hint);
            float target = canShow && !hint.Done ? 1f : 0f;
            float fadeTime = hint.Done ? CompletedFadeOutTime : fadeDuration;

            _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));

            // Fully faded out because it's done, or because its controls went away (dialogue, pause, cutscene).
            if (target == 0f && _group.alpha <= 0f)
            {
                _shownHint = null;
                yield break;
            }
            yield return null;
        }
    }

    Hint NextShowableHint()
    {
        // Hints tied to the current situation (in free cam, aiming) go first, so they show the moment it starts.
        foreach (var hint in hints)
            if (!hint.Done && IsSituational(hint) && CanShow(hint)) return hint;
        foreach (var hint in hints)
            if (!hint.Done && CanShow(hint)) return hint;
        return null;
    }

    static bool IsSituational(Hint hint) =>
        hint.Condition == ShowWhen.InFreeCam || hint.Condition == ShowWhen.AimingSpell;

    bool CanShow(Hint hint)
    {
        if (Time.timeScale <= 0f) return false;
        if (hint.AfterPreviousHint)
        {
            int index = hints.IndexOf(hint);
            if (index > 0 && !hints[index - 1].Done) return false;
        }
        if (!AnyActionEnabled(hint)) return false;
        return ConditionMet(hint);
    }

    static bool AnyActionEnabled(Hint hint)
    {
        foreach (var action in hint.ResolvedActions)
            if (action.enabled) return true;
        return false;
    }

    static bool ConditionMet(Hint hint)
    {
        var input = GlobalInputManager.Instance;
        switch (hint.Condition)
        {
            case ShowWhen.InFreeCam:
                return input.CurrentMode == InputMode.FreeCam;
            case ShowWhen.AimingSpell:
                return input.CurrentMode == InputMode.SpellAim;
            case ShowWhen.HasMultipleHeroes:
                return HeroCount() > 1;
            case ShowWhen.CanAffordSpell:
                return CanAffordCurrentSpell();
            default:
                return true;
        }
    }

    static bool CanAffordCurrentSpell()
    {
        if (ManaSystem.Instance == null || SpellSwapper.Instance == null) return false;
        var spell = SpellSwapper.Instance.CurrentSpell;
        return spell != null && ManaSystem.Instance.PlayerMana >= spell.Cost;
    }

    static int HeroCount()
    {
        if (PlayerSwitch.Instance == null) return 0;
        int count = 0;
        foreach (var player in PlayerSwitch.Instance.Players)
            if (player != null) count++;
        return count;
    }
    #endregion
}
