using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Yarn.Unity;

// Sends Yarn Spinner lines to DialogueManager, so they use the same box, effects and voices as DialogueTrigger dialogue.
// Lives on the DialogueManager prefab; adds a DialogueRunner to itself if there isn't one.
public class YarnDialoguePresenter : DialoguePresenterBase
{
    // '#' starts a hashtag in Yarn, so hex colours are written without it (<color=FF0000>) and TMP's '#' is added back here.
    static readonly Regex HexColourTag = new Regex(@"<(color|mark)=([0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Tooltip("The game's Yarn Project; it includes every .yarn file in its folder and subfolders.")]
    [SerializeField] YarnProject yarnProject;
    [Tooltip("Matched to the speaker name before the colon in a Yarn line (\"Jack: Hi!\"), ignoring case. " +
        "Speakers not in this list still show their name, with the default colours and no voice.")]
    [SerializeField] CharacterDialogueInfo[] characters = new CharacterDialogueInfo[0];
    [Tooltip("Styles usable with <<style Name>> in Yarn, matched by asset name, ignoring case.")]
    [SerializeField] DialoguePresentationStyle[] presentationStyles = new DialoguePresentationStyle[0];
    [Header("Debug")]
    [SerializeField] bool ShowDebugLogs = false;

    public static YarnDialoguePresenter Instance { get; private set; }
    public bool IsRunning => _runner != null && _runner.IsDialogueRunning;

    DialogueRunner _runner;
    bool _commandsRegistered;
    YarnTrigger _trigger;
    // The runner also "completes" the dialogue when it is destroyed (leaving play mode, changing scene).
    bool _shuttingDown;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        _runner = GetComponent<DialogueRunner>();
        if (_runner == null) _runner = gameObject.AddComponent<DialogueRunner>();
        _runner.DialoguePresenters = new DialoguePresenterBase[] { this };
    }

    void OnEnable()
    {
        _shuttingDown = false;
    }

    void OnDisable()
    {
        _shuttingDown = true;
    }

    // trigger (optional) provides the <<event>> list and is told when the node (and anything it jumps to) has finished.
    public async void StartDialogue(string nodeName, YarnTrigger trigger = null)
    {
        if (yarnProject == null)
        {
            Debug.LogError("[YarnDialoguePresenter] No Yarn Project assigned on the DialogueManager prefab.", this);
            return;
        }
        if (IsRunning || DialogueManager.Instance.DialogueActive)
        {
            Debug.LogWarning($"[YarnDialoguePresenter] Can't start '{nodeName}': dialogue is already running.", this);
            return;
        }

        if (_runner.YarnProject != yarnProject) _runner.SetProject(yarnProject);
        RegisterCommands();
        _trigger = trigger;
        if (ShowDebugLogs) Debug.Log($"[YarnDialoguePresenter] starting node '{nodeName}'");

        try
        {
            await _runner.StartDialogue(nodeName);
        }
        catch (OperationCanceledException) { }
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        DialogueManager.Instance.BeginExternalDialogue(TryRunEvent);
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.EndExternalDialogue();

        // Local copy: DialogueEnded may start another dialogue, which sets a new _trigger.
        YarnTrigger trigger = _trigger;
        _trigger = null;
        if (trigger != null && !_shuttingDown) trigger.DialogueEnded?.Invoke();
        return YarnTask.CompletedTask;
    }

    // Registered once, after the runner has its project. Arguments are converted by Yarn (e.g. <<face 3>> → int).
    void RegisterCommands()
    {
        if (_commandsRegistered) return;
        _commandsRegistered = true;

        _runner.AddCommandHandler<int>("face", SetFace);
        _runner.AddCommandHandler<int>("body", SetBody);
        _runner.AddCommandHandler<string>("sound", PlaySound);
        _runner.AddCommandHandler<string>("style", SetStyle);
        _runner.AddCommandHandler<float, float>("position", SetPosition);
        _runner.AddCommandHandler("clear", ClearText);
        _runner.AddCommandHandler<string>("event", RunEvent);
        _runner.AddCommandHandler<int>("relationship", ChangeRelationship);
    }

    // <<relationship 10>> / <<relationship -10>>: changes the relationship of the current interaction.
    void ChangeRelationship(int amount)
    {
        if (RelationshipManager.Instance == null)
        {
            Debug.LogWarning("[YarnDialoguePresenter] <<relationship>> used but there is no RelationshipManager in the scene.");
            return;
        }
        RelationshipManager.Instance.AlterRelationshipStatus(amount);
    }

    void SetFace(int face)
    {
        if (InteractionMinigameManager.Instance == null)
        {
            Debug.LogWarning("[YarnDialoguePresenter] <<face>> used but there is no InteractionMinigameManager in the scene.");
            return;
        }
        InteractionMinigameManager.Instance.SetCharacterFace(face);
    }

    void SetBody(int body)
    {
        if (InteractionMinigameManager.Instance == null)
        {
            Debug.LogWarning("[YarnDialoguePresenter] <<body>> used but there is no InteractionMinigameManager in the scene.");
            return;
        }
        InteractionMinigameManager.Instance.SetCharacterBody(body);
    }

    void PlaySound(string soundName)
    {
        FModAudioManager.instance.PlaySoundByName(soundName);
    }

    // <<style none>> goes back to the dialogue box.
    void SetStyle(string styleName)
    {
        if (string.Equals(styleName, "none", StringComparison.OrdinalIgnoreCase))
        {
            DialogueManager.Instance.SetExternalStyle(null);
            return;
        }
        foreach (DialoguePresentationStyle style in presentationStyles)
        {
            if (style == null || !string.Equals(style.name, styleName, StringComparison.OrdinalIgnoreCase)) continue;
            DialogueManager.Instance.SetExternalStyle(style);
            return;
        }
        Debug.LogWarning($"[YarnDialoguePresenter] <<style {styleName}>>: no Presentation Style with that name in the " +
            "Yarn Dialogue Presenter's Presentation Styles list (DialogueManager prefab).");
    }

    void SetPosition(float x, float y)
    {
        DialogueManager.Instance.SetExternalNextPosition(new Vector2(x, y));
    }

    void ClearText()
    {
        DialogueManager.Instance.ClearExternalText();
    }

    void RunEvent(string eventName)
    {
        if (!TryRunEvent(eventName))
        {
            Debug.LogWarning($"[YarnDialoguePresenter] <<event {eventName}>>: no event with that name on the Yarn Trigger " +
                $"'{(_trigger != null ? _trigger.name : "none")}'. Add it to the trigger's Events list.");
        }
    }

    // Used by <<event Name>> and by <?Name> tags inside lines.
    bool TryRunEvent(string eventName)
    {
        return _trigger != null && _trigger.TryInvokeEvent(eventName);
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string speaker = GetSpeaker(line);
        string text = PrepareText(line);
        // Yarn's compiler tags the line right before a set of options; the choices open as soon as it is revealed.
        bool beforeChoices = HasMetadata(line, "lastline");
        if (ShowDebugLogs) Debug.Log($"[YarnDialoguePresenter] line {line.TextID}: {speaker}: {text}");

        // Finishes when the player confirms after the line is revealed, or when the runner moves on by itself.
        var advanced = new YarnTaskCompletionSource();
        DialogueManager.Instance.ShowExternalLine(FindCharacter(speaker), speaker, text, () => advanced.TrySetResult(), beforeChoices);
        using (token.NextContentToken.Register(() => advanced.TrySetResult()))
        {
            await advanced.Task;
        }
    }

    public override async YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken token)
    {
        // Options whose <<if>> condition is false are hidden.
        var available = new List<DialogueOption>();
        var texts = new List<string>();
        foreach (DialogueOption option in dialogueOptions)
        {
            if (!option.IsAvailable) continue;
            available.Add(option);
            texts.Add(PrepareText(option.Line));
        }
        if (available.Count == 0) return null;
        if (ShowDebugLogs) Debug.Log($"[YarnDialoguePresenter] choices: {string.Join(" | ", texts)}");

        var selected = new YarnTaskCompletionSource<DialogueOption>();
        DialogueManager.Instance.ShowExternalChoices(texts, index => selected.TrySetResult(available[index]));
        using (token.NextContentToken.Register(() => selected.TrySetResult(null)))
        {
            return await selected.Task;
        }
    }

    static string PrepareText(LocalizedLine line)
    {
        string text = GetSpeaker(line) != null ? line.TextWithoutCharacterName.Text : line.Text.Text;
        return HexColourTag.Replace(text.TrimStart(), "<$1=#$2>");
    }

    // Yarn treats everything before the first colon as the speaker. A "name" containing a text tag
    // means the colon was part of the line (e.g. "<palette>Hi! :3</palette>"), so the whole line is kept.
    static string GetSpeaker(LocalizedLine line)
    {
        string speaker = line.CharacterName;
        if (string.IsNullOrWhiteSpace(speaker) || speaker.Contains("<") || speaker.Contains(">")) return null;
        return speaker;
    }

    static bool HasMetadata(LocalizedLine line, string tag)
    {
        foreach (string metadata in line.Metadata)
        {
            if (string.Equals(metadata.TrimStart('#'), tag, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    CharacterDialogueInfo FindCharacter(string speaker)
    {
        if (string.IsNullOrEmpty(speaker)) return null;
        foreach (CharacterDialogueInfo character in characters)
        {
            if (character != null && string.Equals(character.CharacterName, speaker, StringComparison.OrdinalIgnoreCase)) return character;
        }
        return null;
    }
}
