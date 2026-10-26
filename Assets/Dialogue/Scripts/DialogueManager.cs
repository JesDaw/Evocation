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
    [Tooltip("Font for all dialogue text, names and choices, unless the speaker's CharacterDialogueInfo sets its own.")]
    public TMP_FontAsset defaultFontAsset;
    public float defaultFontSize = 36f;
    [Header("Cinematic sentences")]
    [Tooltip("Parent for sentences spawned by slides that use a Presentation Style. " +
        "Must not be inside DialogueBox, which is hidden for those slides.")]
    [SerializeField] RectTransform sentenceContainer;
    [Header("Debug")]
    [SerializeField] bool ShowDebugLogs = false;

    List<Dialogue> dialogueSlides = new List<Dialogue>();
    // Text boxes get a DialogueTextSurface (TMPEffects reveal + effects) the first time they are used.
    Dictionary<TMP_Text, DialogueTextSurface> _surfaces = new Dictionary<TMP_Text, DialogueTextSurface>();
    DialogueTextSurface _currentSurface;
    bool _lineFinished = true;
    string _currentTypingSound;

    // Sentences spawned for Presentation Style slides; returned to the pool when the slide ends.
    class SpawnedSentence
    {
        public DialogueTextSurface Surface;
        public DialogueTextSurface Prefab; // null for sentences built in code
        public Vector2 TargetPosition;
        public float ScrollSpeed; // canvas units per second while sliding to TargetPosition
    }
    List<SpawnedSentence> _activeSentences = new List<SpawnedSentence>();
    List<SpawnedSentence> _sentencePool = new List<SpawnedSentence>();
    List<SpawnedSentence> _retiringSentences = new List<SpawnedSentence>();
    Vector2 _nextSentencePosition;
    // Stacked sentences count from the last positioned one, so the stagger alternates from there.
    float _stackOriginX;
    int _stackIndex = -1;
    float _pendingScrollDistance;
    float _pendingScrollSpeed;

    int slideCount = -1;
    int lineCount = 0;
    DialogueTrigger _dialogueTrigger;
    // Lines sent from outside (YarnDialoguePresenter) instead of a DialogueTrigger's slides.
    bool _externalDialogue;
    System.Action _externalLineAdvance;
    bool _externalAdvanceWhenRevealed;
    System.Action<int> _externalChoiceSelected;
    System.Func<string, bool> _externalTextEvent;
    DialoguePresentationStyle _externalStyle;
    bool _externalUsePosition;
    Vector2 _externalPosition;
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
        if (_externalDialogue)
        {
            System.Action advance = _externalLineAdvance;
            _externalLineAdvance = null;
            advance?.Invoke();
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

    TMP_FontAsset ResolveFont(CharacterDialogueInfo character)
    {
        return character != null && character.fontAsset != null ? character.fontAsset : defaultFontAsset;
    }

    void ApplyCharacterToNameText(CharacterDialogueInfo character)
    {
        TMP_FontAsset font = ResolveFont(character);
        if (font != null) nameText.font = font;

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

    void ApplyCharacterToTextBox(TMP_Text box, CharacterDialogueInfo character)
    {
        box.color = character != null ? character.textColor : defaultTextColor;

        TMP_FontAsset font = ResolveFont(character);
        if (font != null) box.font = font;
        box.fontSize = character != null ? character.fontSize : defaultFontSize;
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

    // Clears a box without giving it a DialogueTextSurface: boxes are often inactive here, and TMPEffects
    // components added to an inactive object didn't animate the first line shown afterwards.
    // The surface is created when a line is shown, after the box has been activated.
    void ClearTextBox(TMP_Text box)
    {
        DialogueTextSurface surface = _surfaces.TryGetValue(box, out DialogueTextSurface cached) && cached != null
            ? cached
            : box.GetComponent<DialogueTextSurface>();
        if (surface != null) surface.Clear();
        else box.text = "";
    }

    DialoguePresentationStyle GetPresentationStyle(Dialogue slide, DialogueLine line)
    {
        if (line.alternitiveTextBox != null || slide.presentationStyle == null) return null;
        if (sentenceContainer == null)
        {
            Debug.LogWarning("[DialogueManager] A slide uses a Presentation Style but no Sentence Container is assigned; using the dialogue box.");
            return null;
        }
        return slide.presentationStyle;
    }

    DialogueTextSurface SpawnSentence(DialoguePresentationStyle style)
    {
        sentenceContainer.gameObject.SetActive(true);

        // Earlier sentences dim, and the oldest leave once the style's limit is reached.
        foreach (SpawnedSentence previous in _activeSentences)
        {
            if (previous.Surface != null) previous.Surface.SetOpacity(style.previousSentenceOpacity, style.fadeDuration);
        }
        while (style.maxVisibleSentences > 0 && _activeSentences.Count >= style.maxVisibleSentences)
        {
            SpawnedSentence oldest = _activeSentences[0];
            _activeSentences.RemoveAt(0);
            if (oldest.Surface == null) continue;
            oldest.Surface.SetOpacity(0f, style.fadeDuration);
            _retiringSentences.Add(oldest);
            if (style.scrollWhenFull) ScrollUpPast(oldest, style);
        }

        SpawnedSentence sentence = null;
        for (int i = _sentencePool.Count - 1; i >= 0; i--)
        {
            if (_sentencePool[i].Surface == null)
            {
                _sentencePool.RemoveAt(i);
                continue;
            }
            if (_sentencePool[i].Prefab != style.sentencePrefab) continue;
            sentence = _sentencePool[i];
            _sentencePool.RemoveAt(i);
            break;
        }
        if (sentence == null) sentence = CreateSentence(style.sentencePrefab);

        // Active before use: TMPEffects needs an active GameObject.
        sentence.Surface.gameObject.SetActive(true);
        sentence.Surface.transform.SetAsLastSibling();
        sentence.Surface.SetOpacity(1f);
        _activeSentences.Add(sentence);
        return sentence.Surface;
    }

    SpawnedSentence CreateSentence(DialogueTextSurface prefab)
    {
        TMP_Text text;
        if (prefab != null)
        {
            text = Instantiate(prefab, sentenceContainer).Text;
        }
        else
        {
            GameObject sentenceObject = new GameObject("Sentence", typeof(RectTransform));
            sentenceObject.transform.SetParent(sentenceContainer, false);
            TextMeshProUGUI tmp = sentenceObject.AddComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            text = tmp;
        }

        // Positioned from the container's top-left corner.
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        return new SpawnedSentence { Surface = GetSurface(text), Prefab = prefab };
    }

    // Uses the screen position (0-1, top-left origin) if given, otherwise stacks below the previous sentence.
    void PlaceSentence(DialogueTextSurface surface, DialoguePresentationStyle style, bool useScreenPosition, Vector2 screenPosition)
    {
        Vector2 position = _nextSentencePosition;
        if (useScreenPosition)
        {
            Rect area = sentenceContainer.rect;
            position = new Vector2(screenPosition.x * area.width, -screenPosition.y * area.height);
            _stackOriginX = position.x;
            _stackIndex = 0;
        }
        else
        {
            _stackIndex++;
            position.x = _stackOriginX + (_stackIndex % 2 == 1 ? style.staggerOffset : 0f);
        }
        SpawnedSentence sentence = _activeSentences[_activeSentences.Count - 1];
        sentence.TargetPosition = position;
        sentence.ScrollSpeed = 0f;
        // A stacked sentence arriving during a scroll starts lower and rises with the others.
        if (!useScreenPosition && _pendingScrollDistance > 0f && !float.IsInfinity(_pendingScrollSpeed))
        {
            surface.Text.rectTransform.anchoredPosition = position - new Vector2(0f, _pendingScrollDistance);
            sentence.ScrollSpeed = _pendingScrollSpeed;
        }
        else surface.Text.rectTransform.anchoredPosition = position;
        _pendingScrollDistance = 0f;
        _nextSentencePosition = position - new Vector2(0f, surface.Text.preferredHeight + style.sentenceSpacing);
    }

    // Teleprompter: everything slides up by the space the removed sentence took, so the new one lands in its place.
    void ScrollUpPast(SpawnedSentence removed, DialoguePresentationStyle style)
    {
        float nextY = _activeSentences.Count > 0 ? _activeSentences[0].TargetPosition.y : _nextSentencePosition.y;
        float distance = removed.TargetPosition.y - nextY;
        if (distance <= 0f) return;

        float speed = style.scrollDuration > 0f ? distance / style.scrollDuration : float.PositiveInfinity;
        foreach (SpawnedSentence sentence in _activeSentences) ScrollSentence(sentence, distance, speed);
        foreach (SpawnedSentence sentence in _retiringSentences) ScrollSentence(sentence, distance, speed);
        _nextSentencePosition.y += distance;
        _pendingScrollDistance += distance;
        _pendingScrollSpeed = speed;
    }

    void ScrollSentence(SpawnedSentence sentence, float distance, float speed)
    {
        sentence.TargetPosition.y += distance;
        sentence.ScrollSpeed = speed;
    }

    void Update()
    {
        MoveSentencesToTargets(_activeSentences);
        MoveSentencesToTargets(_retiringSentences);

        // Sentences fading out because of Max Visible Sentences go back to the pool once invisible.
        for (int i = _retiringSentences.Count - 1; i >= 0; i--)
        {
            SpawnedSentence sentence = _retiringSentences[i];
            if (sentence.Surface != null && sentence.Surface.Opacity > 0f) continue;
            _retiringSentences.RemoveAt(i);
            PoolSentence(sentence);
        }
    }

    // Unscaled time, like the rest of the dialogue; scrolling stops while the game is paused.
    void MoveSentencesToTargets(List<SpawnedSentence> sentences)
    {
        if (UILogic.GameIsPaused) return;
        foreach (SpawnedSentence sentence in sentences)
        {
            if (sentence.Surface == null || sentence.ScrollSpeed <= 0f) continue;
            RectTransform rect = sentence.Surface.Text.rectTransform;
            rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, sentence.TargetPosition,
                sentence.ScrollSpeed * Time.unscaledDeltaTime);
            if (rect.anchoredPosition == sentence.TargetPosition) sentence.ScrollSpeed = 0f;
        }
    }

    void PoolSentence(SpawnedSentence sentence)
    {
        if (sentence.Surface == null) return;
        sentence.Surface.Clear();
        sentence.Surface.gameObject.SetActive(false);
        _sentencePool.Add(sentence);
    }

    void ReleaseSentences()
    {
        foreach (SpawnedSentence sentence in _activeSentences) PoolSentence(sentence);
        foreach (SpawnedSentence sentence in _retiringSentences) PoolSentence(sentence);
        _activeSentences.Clear();
        _retiringSentences.Clear();
        _nextSentencePosition = Vector2.zero;
        _stackOriginX = 0f;
        _stackIndex = -1;
        _pendingScrollDistance = 0f;
    }

    void ClearSlideTextBoxes(int slideIndex)
    {
        ReleaseSentences();

        if (slideIndex < 0 || slideIndex >= dialogueSlides.Count) return;

        Dialogue slide = dialogueSlides[slideIndex];

        foreach (DialogueLine line in slide.Lines)
        {
            TMP_Text box = GetTargetTextBox(line);
            if (box == null) continue;
            ClearTextBox(box);
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

        DialoguePresentationStyle style = GetPresentationStyle(slide, currentLine);

        // Activate the box before its surface is used: TMPEffects needs an active GameObject.
        bool usingUniversalBox = currentLine.alternitiveTextBox == null && style == null;
        DialogueBox.SetActive(usingUniversalBox);

        _currentSurface = style != null ? SpawnSentence(style) : GetSurface(GetTargetTextBox(currentLine));
        TMP_Text targetBox = _currentSurface.Text;

        ApplyCharacterToTextBox(targetBox, character);
        if (style != null) style.ApplyTo(targetBox);

        _currentTypingSound = character != null ? character.Voice : "";

        if (InteractionMinigameManager.Instance != null)
        {
            InteractionMinigameManager.Instance.SetCharacterBody(currentLine.CharacterBody);
            InteractionMinigameManager.Instance.SetCharacterFace(currentLine.CharacterFace);
        }

        _currentSurface.SetRevealDelay(character != null ? character.TextSpeed : defaultTextSpeed);
        _lineFinished = false;

        currentLine.LineStartEvent?.Invoke();

        if (style != null)
        {
            // One surface per sentence, so earlier sentences stay on screen untouched.
            _currentSurface.Show(style.FormatLine(currentLine.Line));
            PlaceSentence(_currentSurface, style, currentLine.useScreenPosition, currentLine.screenPosition);
        }
        // Lines in the same slide and box keep the earlier lines on screen, as before.
        else if (_currentSurface.HasText) _currentSurface.Append(currentLine.Line);
        else _currentSurface.Show(currentLine.Line);

        if (ShowDebugLogs) Debug.Log($"[DialogueManager] slide {slideCount} line {lineCount} -> {targetBox.name}");

        // Empty lines never start the writer, so nothing would report them finished.
        if (!_currentSurface.IsRevealing) OnLineFinished();
    }

    // textEventHandler runs custom <?Name> tags in the lines; it returns false if it has no event with that name.
    public void BeginExternalDialogue(System.Func<string, bool> textEventHandler = null)
    {
        _externalDialogue = true;
        _externalLineAdvance = null;
        _externalTextEvent = textEventHandler;
        _externalStyle = null;
        _externalUsePosition = false;
        _dialogueTrigger = null;
        dialogueSlides = new List<Dialogue>();
        slideCount = -1;
        lineCount = 0;
        DialogueActive = true;

        if (GlobalInputManager.Instance != null) GlobalInputManager.Instance.SetMode(InputMode.Dialogue);
    }

    // Shows one line in the dialogue box. onAdvance runs when the player confirms after the line has finished revealing,
    // or as soon as it has finished revealing if advanceWhenRevealed (used for the line before choices).
    // characterName is shown when there is no CharacterDialogueInfo for the speaker.
    public void ShowExternalLine(CharacterDialogueInfo character, string characterName, string text, System.Action onAdvance,
        bool advanceWhenRevealed = false)
    {
        _externalLineAdvance = onAdvance;
        _externalAdvanceWhenRevealed = advanceWhenRevealed;

        ApplyCharacterToNameText(character);
        if (character == null) nameText.text = characterName ?? "";

        DialoguePresentationStyle style = _externalStyle != null && sentenceContainer != null ? _externalStyle : null;

        // Activate the box before its surface is used: TMPEffects needs an active GameObject.
        DialogueBox.SetActive(style == null);
        _currentSurface = style != null ? SpawnSentence(style) : GetSurface(dialogueText);
        ApplyCharacterToTextBox(_currentSurface.Text, character);
        if (style != null) style.ApplyTo(_currentSurface.Text);

        _currentTypingSound = character != null ? character.Voice : "";
        _currentSurface.SetRevealDelay(character != null ? character.TextSpeed : defaultTextSpeed);
        _lineFinished = false;

        if (style != null)
        {
            _currentSurface.Show(style.FormatLine(text));
            PlaceSentence(_currentSurface, style, _externalUsePosition, _externalPosition);
            _externalUsePosition = false;
        }
        else _currentSurface.Show(text);

        if (ShowDebugLogs) Debug.Log($"[DialogueManager] external line '{text}'");

        if (!_currentSurface.IsRevealing) OnLineFinished();
    }

    // The current line stays on screen while the choices are open. onSelected gets the index into choices.
    public void ShowExternalChoices(IList<string> choices, System.Action<int> onSelected)
    {
        _externalChoiceSelected = onSelected;
        OpenChoiceBox(choices);
    }

    // Following lines become sentences laid out by this style; null goes back to the dialogue box.
    // Changing style clears the sentences on screen, like starting a new slide.
    public void SetExternalStyle(DialoguePresentationStyle style)
    {
        if (style == _externalStyle) return;
        ReleaseSentences();
        _externalStyle = style;
        if (style != null && sentenceContainer == null)
        {
            Debug.LogWarning("[DialogueManager] <<style>> used but no Sentence Container is assigned; using the dialogue box.");
        }
    }

    // Places the next sentence at this screen position (0-1, (0, 0) is top-left); later ones stack below it.
    public void SetExternalNextPosition(Vector2 screenPosition)
    {
        _externalUsePosition = true;
        _externalPosition = screenPosition;
    }

    // Removes the sentences and box text on screen, like starting a new slide.
    public void ClearExternalText()
    {
        ReleaseSentences();
        ClearTextBox(dialogueText);
    }

    public void EndExternalDialogue()
    {
        if (!_externalDialogue) return;
        _externalDialogue = false;
        _externalLineAdvance = null;
        _externalChoiceSelected = null;
        _externalTextEvent = null;
        _externalStyle = null;
        _externalUsePosition = false;
        DeactivateDialogueBox();
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
        if (surface != _currentSurface) return;

        string eventName = args.Tag.Name;
        string value = args.Tag.Parameters != null && args.Tag.Parameters.TryGetValue("", out string v) ? v : "";
        string source = _dialogueTrigger != null ? $"trigger '{_dialogueTrigger.name}'" : "a Yarn line";
        if (ShowDebugLogs) Debug.Log($"[DialogueManager] text event <?{eventName}={value}>");

        switch (eventName.ToLowerInvariant())
        {
            case "face":
            case "body":
                if (!int.TryParse(value, out int id))
                {
                    Debug.LogWarning($"[DialogueManager] <?{eventName}={value}> needs a number, in {source}.");
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

        if (_externalDialogue)
        {
            if (_externalTextEvent == null || !_externalTextEvent(eventName))
            {
                Debug.LogWarning($"[DialogueManager] No event named '{eventName}' for <?{eventName}> in {source}. " +
                    "Add it to the Yarn Trigger's Events list.");
            }
            return;
        }
        if (_dialogueTrigger == null) return;
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
        if (_externalDialogue)
        {
            if (!_externalAdvanceWhenRevealed) return;
            System.Action advance = _externalLineAdvance;
            _externalLineAdvance = null;
            advance?.Invoke();
            return;
        }

        DialogueLine currentLine = dialogueSlides[slideCount].Lines[lineCount];
        currentLine.LineEndEvent?.Invoke();

        if (_dialogueTrigger.EndOfLines.Length > 0 && IsLastLineOfLastSlide()) OpenChoiceBox();
    }

    public void OpenChoiceBox()
    {
        var texts = new List<string>();
        foreach (DialogueChoice choice in _dialogueTrigger.EndOfLines) texts.Add(choice.Text);
        OpenChoiceBox(texts);
    }

    void OpenChoiceBox(IEnumerable<string> choiceTexts)
    {
        ChoiceBox.SetActive(true);
        ChoiceBoxIsOpen = true;
        int ChoiceIndex = 0;
        foreach (string choiceText in choiceTexts)
        {
            GameObject choiceInstance = Instantiate(Choice, ChoiceBox.transform);
            TextMeshProUGUI tmpText = choiceInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                if (defaultFontAsset != null) tmpText.font = defaultFontAsset;
                tmpText.text = choiceText;
            }

            DialogueChoiceWrapper ChoiceNumber = choiceInstance.GetComponentInChildren<DialogueChoiceWrapper>();
            if (ChoiceNumber != null)
            {
                ChoiceNumber.ChoiceIndex = ChoiceIndex;
                ChoiceIndex++;
            }
        }
        GlobalInputManager.Instance.EnableCursor();
    }

    // Called by the choice buttons (DialogueChoiceWrapper).
    public void SelectChoice(int index)
    {
        if (!ChoiceBoxIsOpen) return;
        if (!_externalDialogue)
        {
            EndDialogue(index);
            return;
        }

        System.Action<int> selected = _externalChoiceSelected;
        _externalChoiceSelected = null;
        CloseChoiceBox();
        selected?.Invoke(index);
    }

    void CloseChoiceBox()
    {
        ChoiceBox.SetActive(false);
        ChoiceBoxIsOpen = false;
        foreach (Transform child in ChoiceBox.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
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
        ClearTextBox(dialogueText);
        nameText.text = "";

        CloseChoiceBox();
    }
}