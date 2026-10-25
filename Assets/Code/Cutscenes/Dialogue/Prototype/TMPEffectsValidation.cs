using System.Collections.Generic;
using TMPEffects.Components;
using TMPEffects.TMPEvents;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Throwaway test scene driver, now running everything through DialogueTextSurface.
// Confirm = skip / advance, P = toggle UILogic.GameIsPaused, R = restart.
public class TMPEffectsValidation : MonoBehaviour
{
    [SerializeField] TMP_Text surfaceA;
    [SerializeField] TMP_Text surfaceB;
    [SerializeField] TMP_Text statusText;
    [SerializeField] float inactiveAlpha = 0.35f;
    [SerializeField] float fadeDuration = 0.3f;

    enum Mode { Show, Append }

    struct Step
    {
        public string Label;
        public int Surface; // 0 = A, 1 = B
        public Mode Mode;
        public string Text;
    }

    readonly Step[] _steps =
    {
        new Step { Label = "1/7 Reveal + per-word effects", Surface = 0, Mode = Mode.Show,
            Text = "<+fade>Text reveals progressively, <wave>these words wave</wave>, and <shake>this part shakes</shake>.</+fade>" },
        new Step { Label = "2/7 Append (first sentence must NOT retype)", Surface = 0, Mode = Mode.Append,
            Text = "<+fade>This sentence is <palette>appended</palette> after it.</+fade>" },
        new Step { Label = "3/7 Stacked effects", Surface = 0, Mode = Mode.Show,
            Text = "<+fade>Stacked: <wave><palette>wave and palette together</palette></wave>, then <+grow><funky>a different reveal</funky></+grow>.</+fade>" },
        new Step { Label = "4/7 Events + commands", Surface = 0, Mode = Mode.Show,
            Text = "An event fires here<?expression=angry> and the writer pauses<!wait=1> for a second before it continues." },
        new Step { Label = "5/7 Reuse (no leftover effects)", Surface = 0, Mode = Mode.Show,
            Text = "Plain text on the same surface. Nothing here should still wave, shake or change colour." },
        new Step { Label = "6/7 Two surfaces, previous dimmed", Surface = 1, Mode = Mode.Show,
            Text = "<+move>A second sentence on its own surface. The previous one stays visible, dimmed.</+move>" },
        new Step { Label = "7/7 Swap focus + skip", Surface = 0, Mode = Mode.Show,
            Text = "<+fade>Focus returns to the first surface. <jump>Press confirm mid-line</jump> to test skipping.</+fade>" },
    };

    DialogueTextSurface[] _surfaces;
    InputSystem_Actions _input;
    readonly List<string> _log = new List<string>();
    int _stepIndex = -1;
    bool _wasPaused;

    void Awake()
    {
        _surfaces = new DialogueTextSurface[2];
        TMP_Text[] texts = { surfaceA, surfaceB };
        for (int i = 0; i < 2; i++)
        {
            // Added at runtime on purpose: this is how DialogueManager will attach surfaces to existing text boxes.
            _surfaces[i] = texts[i].GetComponent<DialogueTextSurface>();
            if (_surfaces[i] == null) _surfaces[i] = texts[i].gameObject.AddComponent<DialogueTextSurface>();

            string surfaceName = i == 0 ? "A" : "B";
            _surfaces[i].RevealFinished += _ => Log($"{surfaceName}: reveal finished");
            _surfaces[i].TextEvent += (_, args) => OnTextEvent(surfaceName, args);
        }

        _input = new InputSystem_Actions();
        _input.UI.ConfirmDialogue.performed += OnConfirmPressed;
        _input.UI.Enable();
    }

    void Start()
    {
        for (int i = 0; i < 2; i++)
        {
            _surfaces[i].Clear();
            _surfaces[i].SetOpacity(0f);
        }
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

        if (UILogic.GameIsPaused != _wasPaused)
        {
            _wasPaused = UILogic.GameIsPaused;
            Log(_wasPaused ? "Paused (surfaces stop revealing + animating)" : "Resumed");
        }

        RefreshStatus();
    }

    void OnConfirmPressed(InputAction.CallbackContext context)
    {
        if (!context.performed || UILogic.GameIsPaused) return;

        DialogueTextSurface surface = CurrentSurface();
        if (surface != null && surface.IsRevealing)
        {
            surface.Skip();
            Log("Skipped");
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
            if (i == step.Surface) _surfaces[i].SetOpacity(1f, fadeDuration);
            else if (_surfaces[i].HasText) _surfaces[i].SetOpacity(inactiveAlpha, fadeDuration);
        }

        DialogueTextSurface surface = _surfaces[step.Surface];
        if (step.Mode == Mode.Append) surface.Append(step.Text);
        else surface.Show(step.Text);

        if (_stepIndex == 4) LogReuseCheck(surface);
    }

    void Restart()
    {
        for (int i = 0; i < 2; i++)
        {
            _surfaces[i].Clear();
            _surfaces[i].SetOpacity(0f);
        }
        _log.Clear();
        _stepIndex = -1;
        Advance();
    }

    void LogReuseCheck(DialogueTextSurface surface)
    {
        TMPAnimator animator = surface.GetComponent<TMPAnimator>();
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

    DialogueTextSurface CurrentSurface()
    {
        if (_stepIndex < 0 || _stepIndex >= _steps.Length) return null;
        return _surfaces[_steps[_stepIndex].Surface];
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
        DialogueTextSurface surface = CurrentSurface();
        string state = UILogic.GameIsPaused ? "PAUSED" : (surface != null && surface.IsRevealing ? "revealing" : "idle");
        statusText.text =
            "<b>DialogueTextSurface test</b>   Confirm: skip/next   P: pause   R: restart\n" +
            $"State: {state}\n\n" +
            string.Join("\n", _log);
    }
}
