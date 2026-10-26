using System;
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

    [Tooltip("Leave empty to use TMPEffects' built-in databases from Assets/TMPEffects/Resources.")]
    [SerializeField] TMPAnimationDatabase animationDatabase;
    [SerializeField] TMPCommandDatabase commandDatabase;
    [SerializeField] bool pauseWithGame = true;
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
        Write(_rawText, 0);
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

        // Writer delay is zero across the kept part, so only the new text takes time to reveal.
        Write("<!delay=0>" + kept + "<!delay=default>" + text, keptCharacters);
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

    void Write(string fullText, int alreadyVisibleCharacters)
    {
        if (!EnsureInitialized()) return;
        _writer.StopWriter();
        _pausedWhileRevealing = false;

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
        RevealFinished?.Invoke(this);
    }

    void OnWriterCharacterShown(TMPWriter writer, CharData character)
    {
        CharacterShown?.Invoke(this, character.info.character);
    }
}
