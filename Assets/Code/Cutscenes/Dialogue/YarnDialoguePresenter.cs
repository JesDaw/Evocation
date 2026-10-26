using System;
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
    [Header("Debug")]
    [SerializeField] bool ShowDebugLogs = false;

    public static YarnDialoguePresenter Instance { get; private set; }
    public bool IsRunning => _runner != null && _runner.IsDialogueRunning;

    DialogueRunner _runner;
    Action _onComplete;

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

    // onComplete runs once the node (and anything it jumps to) has finished.
    public async void StartDialogue(string nodeName, Action onComplete = null)
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
        _onComplete = onComplete;
        if (ShowDebugLogs) Debug.Log($"[YarnDialoguePresenter] starting node '{nodeName}'");

        try
        {
            await _runner.StartDialogue(nodeName);
        }
        catch (OperationCanceledException) { }
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        DialogueManager.Instance.BeginExternalDialogue();
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        DialogueManager.Instance.EndExternalDialogue();

        // Local copy: the callback may start another dialogue, which sets a new _onComplete.
        Action onComplete = _onComplete;
        _onComplete = null;
        onComplete?.Invoke();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string speaker = line.CharacterName;
        string text = HexColourTag.Replace(line.TextWithoutCharacterName.Text.TrimStart(), "<$1=#$2>");
        if (ShowDebugLogs) Debug.Log($"[YarnDialoguePresenter] line {line.TextID}: {speaker}: {text}");

        // Finishes when the player confirms after the line is revealed, or when the runner moves on by itself.
        var advanced = new YarnTaskCompletionSource();
        DialogueManager.Instance.ShowExternalLine(FindCharacter(speaker), speaker, text, () => advanced.TrySetResult());
        using (token.NextContentToken.Register(() => advanced.TrySetResult()))
        {
            await advanced.Task;
        }
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
