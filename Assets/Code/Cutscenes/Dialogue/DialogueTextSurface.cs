using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPEffects.CharacterData;
using TMPEffects.Components;
using TMPEffects.Components.Animator;
using TMPEffects.Databases.AnimationDatabase;
using TMPEffects.Databases.CommandDatabase;
using TMPEffects.TMPEvents;
using TMPro;
using UnityEngine;

// One text element on screen: wraps TMP + TMPEffects (TMPWriter for reveal, TMPAnimator for effects).
// Can be added to an existing TMP object at runtime; the TMPEffects components are added if missing.
[RequireComponent(typeof(TMP_Text))]
public class DialogueTextSurface : MonoBehaviour
{
    const string AnimationDatabaseResource = "Databases/DefaultAnimationDatabase";
    const string CommandDatabaseResource = "Databases/DefaultCommandDatabase";

    // Commands/events already ran and show tags already played, so they are removed from text being kept on screen.
    static readonly Regex OneShotTags = new Regex(@"<[!?][^>]*>|</?\+[^>]*>", RegexOptions.Compiled);
    // Underline, strikethrough and highlight are drawn as separate geometry the writer can't hide,
    // so they are left out while revealing and added back once the reveal finishes.
    static readonly Regex DecorationTags = new Regex(@"</?(?:u|s|mark)(?:[ =][^>]*)?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Tooltip("Leave empty to use TMPEffects' built-in databases from Assets/TMPEffects/Resources.")]
    [SerializeField] TMPAnimationDatabase animationDatabase;
    [SerializeField] TMPCommandDatabase commandDatabase;
    [SerializeField] bool pauseWithGame = true;
    [Tooltip("Seconds underlines, strikethroughs and highlights take to fade in once a line finishes revealing.")]
    [SerializeField] float decorationFadeDuration = 0.4f;
    [Header("Debug")]
    [SerializeField] bool ShowDebugLogs = false;

    public event Action<DialogueTextSurface> RevealFinished;
    public event Action<DialogueTextSurface, TMPEventArgs> TextEvent;
    public event Action<DialogueTextSurface, char> CharacterShown;

    TMP_Text _text;
    TMPWriter _writer;
    TMPAnimator _animator;
    CanvasGroup _canvasGroup;
    string _rawText = "";
    bool _componentsCached;
    bool _initialized;
    bool _pausedWhileRevealing;
    bool _wasPaused;
    bool _decorationsPending;
    // Mesh vertices that belong to no character (underline/strikethrough/highlight geometry), with their full alpha.
    readonly List<(int mesh, int vertex, byte alpha)> _decorationVertices = new List<(int, int, byte)>();
    float _decorationFadeTime = -1f;
    float _targetAlpha = 1f;
    float _fadeSpeed;

    public TMP_Text Text
    {
        get
        {
            CacheComponents();
            return _text;
        }
    }
    public bool IsRevealing => _writer != null && (_writer.IsWriting || _pausedWhileRevealing);
    public bool HasText => !string.IsNullOrEmpty(_rawText);
    public float Opacity => _canvasGroup != null ? _canvasGroup.alpha : 1f;

    void Awake()
    {
        CacheComponents();
    }

    void Start()
    {
        EnsureInitialized();
    }

    void OnDestroy()
    {
        if (_writer == null) return;
        _writer.OnTextEvent.RemoveListener(OnWriterTextEvent);
        _writer.OnFinishWriter.RemoveListener(OnWriterFinished);
        _writer.OnCharacterShown.RemoveListener(OnWriterCharacterShown);
    }

    void Update()
    {
        bool paused = pauseWithGame && UILogic.GameIsPaused;
        HandlePause(paused);

        // Animations are driven here so they use unscaled time and stop while paused.
        if (!paused && _initialized && _animator.isActiveAndEnabled) _animator.UpdateAnimations(Time.unscaledDeltaTime);

        // After the animator, which only rewrites character vertices, so the decoration alpha set here is kept.
        if (!paused && _decorationFadeTime >= 0f)
        {
            _decorationFadeTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(_decorationFadeTime / decorationFadeDuration);
            SetDecorationAlpha(progress);
            if (progress >= 1f) _decorationFadeTime = -1f;
        }

        if (_canvasGroup != null && !Mathf.Approximately(_canvasGroup.alpha, _targetAlpha))
        {
            _canvasGroup.alpha = _fadeSpeed <= 0f
                ? _targetAlpha
                : Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, _fadeSpeed * Time.unscaledDeltaTime);
        }
    }

    // Safe to call while the GameObject is inactive (Awake has not run yet in that case).
    void CacheComponents()
    {
        if (_componentsCached) return;
        _componentsCached = true;

        _text = GetComponent<TMP_Text>();
        _writer = GetComponent<TMPWriter>();
        if (_writer == null) _writer = gameObject.AddComponent<TMPWriter>();
        _animator = GetComponent<TMPAnimator>();
        if (_animator == null) _animator = gameObject.AddComponent<TMPAnimator>();

        // The surface decides when writing starts; the game pauses with UILogic.GameIsPaused, not timeScale.
        _writer.WriteOnStart = false;
        _writer.WriteOnNewText = false;
        _writer.UseScaledTime = false;

        // TMPWriter's events have no initializers; normally Unity's serializer creates them, but not guaranteed here.
        _writer.OnTextEvent ??= new TMPEvent();
        _writer.OnFinishWriter ??= new UnityEngine.Events.UnityEvent<TMPWriter>();
        _writer.OnCharacterShown ??= new UnityEngine.Events.UnityEvent<TMPWriter, CharData>();

        _writer.OnTextEvent.AddListener(OnWriterTextEvent);
        _writer.OnFinishWriter.AddListener(OnWriterFinished);
        _writer.OnCharacterShown.AddListener(OnWriterCharacterShown);
    }

    // TMPEffects' SetDatabase throws before the components' OnEnable, so this needs an active GameObject
    // and runs in Start or on first use, never in Awake.
    bool EnsureInitialized()
    {
        CacheComponents();
        if (_initialized) return true;
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning($"[DialogueTextSurface] {name} is inactive; activate it before showing text.");
            return false;
        }
        _initialized = true;

        if (animationDatabase == null) animationDatabase = Resources.Load<TMPAnimationDatabase>(AnimationDatabaseResource);
        if (commandDatabase == null) commandDatabase = Resources.Load<TMPCommandDatabase>(CommandDatabaseResource);
        if (animationDatabase == null || commandDatabase == null)
        {
            Debug.LogError($"[DialogueTextSurface] {name}: TMPEffects databases not found, effect tags will be ignored. " +
                "Import the TMPEffects required resources into Assets/TMPEffects.");
        }

        _animator.SetDatabase(animationDatabase);
        _writer.SetDatabase(commandDatabase);
        _animator.SetUpdateFrom(UpdateFrom.Script);
        return true;
    }

    // Seconds between revealed characters for the next Show/Append (<!delay> tags in the text still override it).
    public void SetRevealDelay(float secondsPerCharacter)
    {
        CacheComponents();
        _writer.DefaultDelays.SetDelay(Mathf.Max(0f, secondsPerCharacter));
    }

    // Replace everything on the surface and reveal it.
    public void Show(string text)
    {
        _rawText = text ?? "";
        _decorationsPending = false;
        Write(WithoutDecorations(_rawText), 0);
    }

    // Keep the current text on screen and reveal only the new part after it.
    public void Append(string text, string separator = " ")
    {
        if (!HasText)
        {
            Show(text);
            return;
        }

        if (!EnsureInitialized()) return;
        string kept = OneShotTags.Replace(_rawText, "") + separator;
        int keptCharacters = _text.textInfo.characterCount + separator.Length;
        _rawText = kept + (text ?? "");
        _decorationsPending = false;

        // Writer delay is zero across the kept part, so only the new text takes time to reveal.
        Write("<!delay=0>" + kept + "<!delay=default>" + WithoutDecorations(text), keptCharacters);
    }

    // Finish the current reveal immediately (respects <!skippable=false> sections).
    public void Skip()
    {
        if (!_initialized) return;
        if (_pausedWhileRevealing)
        {
            _pausedWhileRevealing = false;
            _writer.SkipWriter();
            return;
        }
        if (_writer.IsWriting && _writer.MaySkip) _writer.SkipWriter();
    }

    // Remove all text and runtime state so the surface can be reused. Works while inactive.
    public void Clear()
    {
        CacheComponents();
        if (_initialized && isActiveAndEnabled) _writer.StopWriter();
        _pausedWhileRevealing = false;
        _decorationsPending = false;
        _decorationFadeTime = -1f;
        _rawText = "";
        _text.text = "";
        if (isActiveAndEnabled) _text.ForceMeshUpdate();
        if (_initialized && isActiveAndEnabled) _animator.ResetTime();
    }

    // Opacity for the whole surface, e.g. dimming previous sentences. fadeDuration in unscaled seconds.
    public void SetOpacity(float alpha, float fadeDuration = 0f)
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        _targetAlpha = alpha;
        _fadeSpeed = fadeDuration > 0f ? Mathf.Abs(_canvasGroup.alpha - alpha) / fadeDuration : 0f;
        if (fadeDuration <= 0f) _canvasGroup.alpha = alpha;
    }

    // Sets _decorationsPending when anything was removed, so OnWriterFinished restores it.
    string WithoutDecorations(string text)
    {
        if (string.IsNullOrEmpty(text) || !DecorationTags.IsMatch(text)) return text;
        _decorationsPending = true;
        return DecorationTags.Replace(text, "");
    }

    // Re-sets the finished text with its decoration tags, everything visible and without replaying show animations.
    void RestoreDecorations()
    {
        _decorationsPending = false;
        _text.text = OneShotTags.Replace(_rawText, "");
        _text.ForceMeshUpdate();
        _writer.Show(0, _text.textInfo.characterCount, true);

        CollectDecorationVertices();
        if (decorationFadeDuration > 0f && _decorationVertices.Count > 0)
        {
            _decorationFadeTime = 0f;
            SetDecorationAlpha(0f);
        }
    }

    void CollectDecorationVertices()
    {
        _decorationVertices.Clear();
        TMP_TextInfo info = _text.textInfo;
        var characterVertices = new HashSet<(int, int)>();
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible) continue;
            for (int j = 0; j < 4; j++) characterVertices.Add((character.materialReferenceIndex, character.vertexIndex + j));
        }

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            Color32[] colors = info.meshInfo[m].colors32;
            if (colors == null) continue;
            int count = Mathf.Min(info.meshInfo[m].vertexCount, colors.Length);
            for (int v = 0; v < count; v++)
            {
                if (!characterVertices.Contains((m, v))) _decorationVertices.Add((m, v, colors[v].a));
            }
        }
    }

    void SetDecorationAlpha(float fraction)
    {
        TMP_TextInfo info = _text.textInfo;
        foreach (var (mesh, vertex, alpha) in _decorationVertices)
        {
            if (mesh >= info.meshInfo.Length) continue;
            Color32[] colors = info.meshInfo[mesh].colors32;
            if (colors == null || vertex >= colors.Length) continue;
            colors[vertex].a = (byte)(alpha * fraction);
        }
        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    void Write(string fullText, int alreadyVisibleCharacters)
    {
        if (!EnsureInitialized()) return;
        _writer.StopWriter();
        _pausedWhileRevealing = false;
        _decorationFadeTime = -1f;

        _text.text = fullText;
        _text.ForceMeshUpdate(); // processes the tags now, which resets and hides the writer's text

        if (alreadyVisibleCharacters > 0) _writer.Show(0, Mathf.Min(alreadyVisibleCharacters, _text.textInfo.characterCount), true);
        _writer.StartWriter();

        if (ShowDebugLogs) Debug.Log($"[DialogueTextSurface] {name}: writing '{fullText}' (kept {alreadyVisibleCharacters} chars)");
    }

    void HandlePause(bool paused)
    {
        if (paused == _wasPaused) return;
        _wasPaused = paused;

        if (paused && _writer.IsWriting)
        {
            _writer.StopWriter();
            _pausedWhileRevealing = true;
        }
        else if (!paused && _pausedWhileRevealing)
        {
            _pausedWhileRevealing = false;
            _writer.StartWriter();
        }
    }

    void OnWriterTextEvent(TMPEventArgs args)
    {
        if (ShowDebugLogs) Debug.Log($"[DialogueTextSurface] {name}: event '{args.Tag.Name}'");
        TextEvent?.Invoke(this, args);
    }

    void OnWriterFinished(TMPWriter writer)
    {
        if (_decorationsPending) RestoreDecorations();
        RevealFinished?.Invoke(this);
    }

    void OnWriterCharacterShown(TMPWriter writer, CharData character)
    {
        CharacterShown?.Invoke(this, character.info.character);
    }
}
