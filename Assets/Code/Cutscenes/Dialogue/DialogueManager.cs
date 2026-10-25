using System.Collections.Generic;
using TMPEffects.TMPEvents;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    [Header("Text box referances")]
    [SerializeField] GameObject DialogueBox;
    [SerializeField] GameObject ChoiceBox;
    [SerializeField] GameObject Choice;
    bool ChoiceBoxIsOpen = false;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    [Header("Text box customisation")]
    public Color defaultTextColor = new Color(0.99f, 0.99f, 0.99f, 1f);
    public Color defaultNameColor = new Color(0f, 0f, 0f, 1f);
    public float defaultTextSpeed = .05f;
    public string defaultTypingSound = "dialogueType";
    public TMP_FontAsset defaultFontAsset;
    public float defaultFontSize = 36f;
    [Header("Debug")]
    [SerializeField] bool ShowDebugLogs = false;

    List<Dialogue> dialogueSlides = new List<Dialogue>();
    // Text boxes get a DialogueTextSurface (TMPEffects reveal + effects) the first time they are used.
    Dictionary<TMP_Text, DialogueTextSurface> _surfaces = new Dictionary<TMP_Text, DialogueTextSurface>();
    DialogueTextSurface _currentSurface;
    bool _lineFinished = true;
    string _currentTypingSound;

    int slideCount = -1; 
    int lineCount = 0;
    DialogueTrigger _dialogueTrigger;
    public bool DialogueActive { get; private set; }
    public static DialogueManager Instance { get; private set; }

    void SubscribeToInputs()
    {
        if (GlobalInputManager.Instance == null)
        {
            Debug.LogWarning($"DialogueManager didnt find the GlobalInputManager");
            return;
        }
        var uiActions = GlobalInputManager.Instance.InputActions.UI;
        uiActions.ConfirmDialogue.performed += OnConfirmDialoguePressed;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
        }
        else
        {
            Debug.Log($"[DialogueManager] cant find globalinputmanager");
        }
    }

    void OnDisable()
    {
        UnsubscribeFromInputs();
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null) return;
        var uiActions = GlobalInputManager.Instance.InputActions.UI;
        uiActions.ConfirmDialogue.performed -= OnConfirmDialoguePressed;
    }

    public void StartDialogue(List<Dialogue> slides, DialogueTrigger trigger)
    {
        dialogueSlides = slides;
        _dialogueTrigger = trigger;
        slideCount = -1;
        lineCount = 0;
        DialogueActive = true;

        if (ShowDebugLogs) Debug.Log($"slides = {slides.Count}, trigger = {trigger.gameObject.name}");

        GlobalInputManager.Instance.SetMode(InputMode.Dialogue);

        DisplayNextSlide();
    }

    public void OnConfirmDialoguePressed(InputAction.CallbackContext context)
    {
        if (!context.performed || !DialogueActive || UILogic.GameIsPaused || ChoiceBoxIsOpen) return;

        if (ShowDebugLogs) Debug.Log("Dialogue confirm button pressed");
        OnConfirmDialoguePressedLogic();
    }

    void OnConfirmDialoguePressedLogic()
    {
        if (!_lineFinished)
        {
            SkipToEndOfLine();
            return;
        }
        if (IsLastLineOfLastSlide())
        {
            EndDialogue();
            return;
        }
        DisplayNextLine();
    }

    bool IsLastLineOfLastSlide()
    {
        if (slideCount < 0 || slideCount >= dialogueSlides.Count) return true;
        return slideCount >= dialogueSlides.Count - 1
            && lineCount >= dialogueSlides[slideCount].Lines.Length - 1;
    }

    void DisplayNextLine()
    {
        lineCount++;

        if (lineCount >= dialogueSlides[slideCount].Lines.Length)
        {
            DisplayNextSlide();
            return;
        }

        ShowLine();
    }

    void DisplayNextSlide()
    {
        ClearSlideTextBoxes(slideCount);

        slideCount++;

        if (slideCount >= dialogueSlides.Count)
        {
            EndDialogue();
            return;
        }

        lineCount = -1;

        Dialogue slide = dialogueSlides[slideCount];

        if (slide.Lines == null || slide.Lines.Length == 0)
        {
            Debug.LogWarning($"[DialogueManager] Slide {slideCount} has no lines, skipping.");
            DisplayNextSlide();
            return;
        }

        ShowSlideCanvasGroup(slideCount);

        if (ShowDebugLogs) Debug.Log($"[DialogueManager] entering slide {slideCount}");

        DisplayNextLine();
    }

    void ApplyCharacterToNameText(CharacterDialogueInfo character)
    {
        if (character != null)
        {
            nameText.text = character.CharacterName;
            nameText.color = character.nameColor;
        }
        else
        {
            nameText.text = "";
            nameText.color = defaultNameColor;
        }
    }

    TMP_Text GetTargetTextBox(DialogueLine line)
    {
        return line.alternitiveTextBox != null ? line.alternitiveTextBox : dialogueText;
    }

    DialogueTextSurface GetSurface(TMP_Text box)
    {
        if (_surfaces.TryGetValue(box, out DialogueTextSurface surface) && surface != null) return surface;

        surface = box.GetComponent<DialogueTextSurface>();
        if (surface == null) surface = box.gameObject.AddComponent<DialogueTextSurface>();
        surface.RevealFinished += OnSurfaceRevealFinished;
        surface.CharacterShown += OnSurfaceCharacterShown;
        surface.TextEvent += OnSurfaceTextEvent;
        _surfaces[box] = surface;
        return surface;
    }

    void ClearSlideTextBoxes(int slideIndex)
    {
        if (slideIndex < 0 || slideIndex >= dialogueSlides.Count) return;

        Dialogue slide = dialogueSlides[slideIndex];

        foreach (DialogueLine line in slide.Lines)
        {
            TMP_Text box = GetTargetTextBox(line);
            if (box == null) continue;
            GetSurface(box).Clear();
        }

        if (slide.alternitiveCanvasGroup != null)
        {
            slide.alternitiveCanvasGroup.alpha = 0f;
            slide.alternitiveCanvasGroup.interactable = false;
            slide.alternitiveCanvasGroup.blocksRaycasts = false;
        }
    }

    void ShowSlideCanvasGroup(int slideIndex)
    {
        if (slideIndex < 0 || slideIndex >= dialogueSlides.Count) return;

        CanvasGroup group = dialogueSlides[slideIndex].alternitiveCanvasGroup;
        if (group == null) return;

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    CharacterDialogueInfo ResolveCharacter(Dialogue slide, DialogueLine line)
    {
        return line.CharacterOverride != null ? line.CharacterOverride : slide.Character;
    }

    void ShowLine()
    {
        Dialogue slide = dialogueSlides[slideCount];
        DialogueLine currentLine = slide.Lines[lineCount];
        CharacterDialogueInfo character = ResolveCharacter(slide, currentLine);

        ApplyCharacterToNameText(character);

        TMP_Text targetBox = GetTargetTextBox(currentLine);

        // Activate the box before its surface is used: TMPEffects needs an active GameObject.
        bool usingUniversalBox = currentLine.alternitiveTextBox == null;
        DialogueBox.SetActive(usingUniversalBox);

        targetBox.color = character != null ? character.textColor : defaultTextColor;

        if (character != null && character.fontAsset != null)
        {
            targetBox.font = character.fontAsset;
        }
        targetBox.fontSize = character != null ? character.fontSize : defaultFontSize;

        _currentTypingSound = character != null ? character.Voice : "";

        if (InteractionMinigameManager.Instance != null)
        {
            InteractionMinigameManager.Instance.SetCharacterBody(currentLine.CharacterBody);
            InteractionMinigameManager.Instance.SetCharacterFace(currentLine.CharacterFace);
        }

        _currentSurface = GetSurface(targetBox);
        _currentSurface.SetRevealDelay(character != null ? character.TextSpeed : defaultTextSpeed);
        _lineFinished = false;

        currentLine.LineStartEvent?.Invoke();

        // Lines in the same slide and box keep the earlier lines on screen, as before.
        if (_currentSurface.HasText) _currentSurface.Append(currentLine.Line);
        else _currentSurface.Show(currentLine.Line);

        if (ShowDebugLogs) Debug.Log($"[DialogueManager] slide {slideCount} line {lineCount} -> {targetBox.name}");

        // Empty lines never start the writer, so nothing would report them finished.
        if (!_currentSurface.IsRevealing) OnLineFinished();
    }

    void SkipToEndOfLine()
    {
        if (_currentSurface == null) return;
        _currentSurface.Skip();
        if (!_currentSurface.IsRevealing) OnLineFinished();
    }

    void OnSurfaceRevealFinished(DialogueTextSurface surface)
    {
        if (surface == _currentSurface) OnLineFinished();
    }

    // <?face=N>, <?body=N> and <?sound=name> are built in; any other <?Name> runs the matching
    // TextEvents entry on the current DialogueTrigger. Events still fire when the line is skipped.
    void OnSurfaceTextEvent(DialogueTextSurface surface, TMPEventArgs args)
    {
        if (surface != _currentSurface || _dialogueTrigger == null) return;

        string eventName = args.Tag.Name;
        string value = args.Tag.Parameters != null && args.Tag.Parameters.TryGetValue("", out string v) ? v : "";
        if (ShowDebugLogs) Debug.Log($"[DialogueManager] text event <?{eventName}={value}>");

        switch (eventName.ToLowerInvariant())
        {
            case "face":
            case "body":
                if (!int.TryParse(value, out int id))
                {
                    Debug.LogWarning($"[DialogueManager] <?{eventName}={value}> needs a number, in trigger '{_dialogueTrigger.name}'.");
                    return;
                }
                if (InteractionMinigameManager.Instance == null)
                {
                    Debug.LogWarning($"[DialogueManager] <?{eventName}> used but there is no InteractionMinigameManager in the scene.");
                    return;
                }
                if (eventName.ToLowerInvariant() == "face") InteractionMinigameManager.Instance.SetCharacterFace(id);
                else InteractionMinigameManager.Instance.SetCharacterBody(id);
                return;
            case "sound":
                FModAudioManager.instance.PlaySoundByName(value);
                return;
        }

        if (!_dialogueTrigger.TryInvokeTextEvent(eventName))
        {
            Debug.LogWarning($"[DialogueManager] No Text Event named '{eventName}' on trigger '{_dialogueTrigger.name}'. " +
                "Add it to the trigger's Text Events list.");
        }
    }

    void OnSurfaceCharacterShown(DialogueTextSurface surface, char character)
    {
        if (surface != _currentSurface || string.IsNullOrEmpty(_currentTypingSound) || char.IsWhiteSpace(character)) return;
        FModAudioManager.instance.PlaySoundByName(_currentTypingSound);
    }

    // Runs once per line, whether the reveal finished on its own or was skipped.
    void OnLineFinished()
    {
        if (_lineFinished || !DialogueActive) return;
        _lineFinished = true;

        DialogueLine currentLine = dialogueSlides[slideCount].Lines[lineCount];
        currentLine.LineEndEvent?.Invoke();

        if (_dialogueTrigger.EndOfLines.Length > 0 && IsLastLineOfLastSlide()) OpenChoiceBox();
    }

    public void OpenChoiceBox()
    {
        ChoiceBox.SetActive(true);
        ChoiceBoxIsOpen = true;
        int ChoiceIndex = 0;
        foreach (DialogueChoice choice in _dialogueTrigger.EndOfLines)
        {
            GameObject choiceInstance = Instantiate(Choice, ChoiceBox.transform);
            TextMeshProUGUI tmpText = choiceInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null) tmpText.text = choice.Text;

            DialogueChoiceWrapper ChoiceNumber = choiceInstance.GetComponentInChildren<DialogueChoiceWrapper>();
            if (ChoiceNumber != null)
            {
                ChoiceNumber.ChoiceIndex = ChoiceIndex;
                ChoiceIndex++;
            }
        }
        GlobalInputManager.Instance.EnableCursor();
    }

    public void EndDialogue(int CurrentChoiceIndex = 0)
    {
        if (DialogueActive == false) return;
        // Keep a local reference: DefultEvent may start another dialogue, which replaces _dialogueTrigger.
        DialogueTrigger endingTrigger = _dialogueTrigger;
        DeactivateDialogueBox();
        endingTrigger.EndDialogueDefultEvent();
        if (endingTrigger.EndOfLines.Length > 0)
        {
            endingTrigger.EndDialogue(CurrentChoiceIndex);
        }
    }

    public void DeactivateDialogueBox()
    {
        if (ShowDebugLogs) Debug.Log($"disabling game dialogue box");

        ClearSlideTextBoxes(slideCount);

        DialogueBox.SetActive(false);
        DialogueActive = false;
        slideCount = -1;
        lineCount = 0;
        _currentSurface = null;
        _lineFinished = true;
        GetSurface(dialogueText).Clear();
        nameText.text = "";

        ChoiceBox.SetActive(false);
        ChoiceBoxIsOpen = false;
        foreach (Transform child in ChoiceBox.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}