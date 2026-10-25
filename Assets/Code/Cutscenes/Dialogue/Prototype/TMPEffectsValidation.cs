using System.Collections.Generic;
using TMPEffects.Components;
using TMPEffects.Databases.AnimationDatabase;
using TMPEffects.Databases.CommandDatabase;
using TMPEffects.TMPEvents;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Throwaway Phase 2 test: checks what TMPEffects handles on its own before we design the real text surface.
// Confirm = skip / advance, P = toggle UILogic.GameIsPaused, R = restart.
public class TMPEffectsValidation : MonoBehaviour
{
    [SerializeField] TMP_Text surfaceA;
    [SerializeField] TMP_Text surfaceB;
    [SerializeField] TMP_Text statusText;
    [Tooltip("Assigned at runtime: TMPEffects' 'use default database' only works through its Inspector.")]
    [SerializeField] TMPAnimationDatabase animationDatabase;
    [SerializeField] TMPCommandDatabase commandDatabase;
    [SerializeField] float inactiveAlpha = 0.35f;
    [SerializeField] float alphaFadeSpeed = 4f;

    struct Step
    {
        public string Label;
        public int Surface; // 0 = A, 1 = B
        public string Text;
    }

    readonly Step[] _steps =
    {
        new Step { Label = "1/6 Reveal + per-word effects", Surface = 0,
            Text = "<+fade>Text reveals progressively, <wave>these words wave</wave>, and <shake>this part shakes</shake>.</+fade>" },
        new Step { Label = "2/6 Stacked effects", Surface = 0,
            Text = "<+fade>Stacked: <wave><palette>wave and palette together</palette></wave>, then <+grow><funky>a different reveal</funky></+grow>.</+fade>" },
        new Step { Label = "3/6 Events + commands", Surface = 0,
            Text = "An event fires here<?expression=angry> and the writer pauses<!wait=1> for a second before it continues." },
        new Step { Label = "4/6 Reuse (no leftover effects)", Surface = 0,
            Text = "Plain text on the same surface. Nothing here should still wave, shake or change colour." },
        new Step { Label = "5/6 Two surfaces, previous dimmed", Surface = 1,
            Text = "<+move>A second sentence on its own surface. The previous one stays visible, dimmed.</+move>" },
        new Step { Label = "6/6 Swap focus + skip", Surface = 0,
            Text = "<+fade>Focus returns to the first surface. <jump>Press confirm mid-line</jump> to test skipping.</+fade>" },
    };

    TMP_Text[] _surfaces;
    TMPWriter[] _writers;
    TMPAnimator[] _animators;
    CanvasGroup[] _groups;
    float[] _targetAlpha;
    bool[] _wasWritingBeforePause;

    InputSystem_Actions _input;
    readonly List<string> _log = new List<string>();
    int _stepIndex = -1;
    bool _wasPaused;

    void Awake()
    {
        _surfaces = new[] { surfaceA, surfaceB };
        _writers = new TMPWriter[2];
        _animators = new TMPAnimator[2];
        _groups = new CanvasGroup[2];
        _targetAlpha = new float[2];
        _wasWritingBeforePause = new bool[2];

        for (int i = 0; i < 2; i++)
        {
            _writers[i] = _surfaces[i].GetComponent<TMPWriter>();
            _animators[i] = _surfaces[i].GetComponent<TMPAnimator>();
            _groups[i] = _surfaces[i].GetComponent<CanvasGroup>();

            if (animationDatabase != null) _animators[i].SetDatabase(animationDatabase);
            if (commandDatabase != null) _writers[i].SetDatabase(commandDatabase);
            if (_animators[i].Database == null || _writers[i].Database == null)
            {
                Debug.LogError($"[TMPEffectsValidation] {_surfaces[i].name} has no TMPEffects database, so no effects or commands will work. " +
                    "Rebuild the scene via Tools > Dialogue Prototype.");
            }

            // The game pauses with UILogic.GameIsPaused, not Time.timeScale, so the writer runs on unscaled time.
            _writers[i].UseScaledTime = false;

            string surfaceName = i == 0 ? "A" : "B";
            _writers[i].OnTextEvent.AddListener(args => OnTextEvent(surfaceName, args));
            _writers[i].OnFinishWriter.AddListener(_ => Log($"{surfaceName}: writer finished"));
            _writers[i].OnSkipWriter.AddListener((_, index) => Log($"{surfaceName}: skipped to index {index}"));

            _surfaces[i].text = "";
            _groups[i].alpha = 0f;
        }

        _input = new InputSystem_Actions();
        _input.UI.ConfirmDialogue.performed += OnConfirmPressed;
        _input.UI.Enable();
    }

    void Start()
    {
        Advance();
    }

    void OnDestroy()
    {
        if (_input == null) return;
        _input.UI.ConfirmDialogue.performed -= OnConfirmPressed;
        _input.Dispose();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.pKey.wasPressedThisFrame) UILogic.GameIsPaused = !UILogic.GameIsPaused;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame) Restart();

        HandlePause();

        for (int i = 0; i < 2; i++)
        {
            _groups[i].alpha = Mathf.MoveTowards(_groups[i].alpha, _targetAlpha[i], alphaFadeSpeed * Time.unscaledDeltaTime);
        }

        RefreshStatus();
    }

    void OnConfirmPressed(InputAction.CallbackContext context)
    {
        if (!context.performed || UILogic.GameIsPaused) return;

        TMPWriter writer = CurrentWriter();
        if (writer != null && writer.IsWriting)
        {
            writer.SkipWriter();
            return;
        }
        Advance();
    }

    void Advance()
    {
        _stepIndex++;
        if (_stepIndex >= _steps.Length)
        {
            Log("All steps done. Press R to restart.");
            _stepIndex = _steps.Length - 1;
            return;
        }

        Step step = _steps[_stepIndex];
        Log($"--- {step.Label}");

        for (int i = 0; i < 2; i++)
        {
            bool isActive = i == step.Surface;
            bool hasText = !string.IsNullOrEmpty(_surfaces[i].text);
            _targetAlpha[i] = isActive ? 1f : (hasText ? inactiveAlpha : 0f);
        }

        // WriteOnNewText (on by default) restarts the writer whenever the text content changes.
        _surfaces[step.Surface].text = step.Text;

        if (_stepIndex == 3) LogReuseCheck(step.Surface);
    }

    void Restart()
    {
        for (int i = 0; i < 2; i++)
        {
            _surfaces[i].text = "";
            _groups[i].alpha = 0f;
            _targetAlpha[i] = 0f;
        }
        _log.Clear();
        _stepIndex = -1;
        Advance();
    }

    void HandlePause()
    {
        bool paused = UILogic.GameIsPaused;
        if (paused == _wasPaused) return;
        _wasPaused = paused;

        for (int i = 0; i < 2; i++)
        {
            if (paused)
            {
                _wasWritingBeforePause[i] = _writers[i].IsWriting;
                _writers[i].StopWriter();
                _animators[i].StopAnimating();
            }
            else
            {
                if (_wasWritingBeforePause[i]) _writers[i].StartWriter();
                _animators[i].StartAnimating();
            }
        }
        Log(paused ? "Paused (writer + animator stopped)" : "Resumed");
    }

    void LogReuseCheck(int surface)
    {
        TMPAnimator animator = _animators[surface];
        int basic = animator.BasicTags?.Count ?? -1;
        int show = animator.ShowTags?.Count ?? -1;
        string result = basic == 0 && show == 0 ? "PASS" : "CHECK";
        Log($"Reuse check {result}: basic tags = {basic}, show tags = {show}");
    }

    void OnTextEvent(string surfaceName, TMPEventArgs args)
    {
        string value = args.Tag.Parameters != null && args.Tag.Parameters.TryGetValue("", out string v) ? v : "";
        Log($"{surfaceName}: event '?{args.Tag.Name}' value '{value}' at index {args.Indices.StartIndex}");
    }

    TMPWriter CurrentWriter()
    {
        if (_stepIndex < 0 || _stepIndex >= _steps.Length) return null;
        return _writers[_steps[_stepIndex].Surface];
    }

    void Log(string message)
    {
        Debug.Log($"[TMPEffectsValidation] {message}");
        _log.Add(message);
        if (_log.Count > 8) _log.RemoveAt(0);
    }

    void RefreshStatus()
    {
        if (statusText == null) return;
        TMPWriter writer = CurrentWriter();
        string state = UILogic.GameIsPaused ? "PAUSED" : (writer != null && writer.IsWriting ? "writing" : "idle");
        statusText.text =
            "<b>TMPEffects validation</b>   Confirm: skip/next   P: pause   R: restart\n" +
            $"State: {state}\n\n" +
            string.Join("\n", _log);
    }
}
