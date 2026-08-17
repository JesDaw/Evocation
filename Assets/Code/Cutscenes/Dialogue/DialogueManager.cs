using System.Collections;
using System.Collections.Generic;
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
    Coroutine _typeLineCoroutine;
    string _currentLine;
    string _currentPrefix; 
    TMP_Text _currentTargetBox;
    Dictionary<TMP_Text, string> _boxAccumulatedText = new Dictionary<TMP_Text, string>();

    int slideCount = -1; 
    int lineCount = 0;
    DailogueTrigger _dailogueTrigger;
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

    public void StartDialogue(List<Dialogue> slides, DailogueTrigger trigger)
    {
        dialogueSlides = slides;
        _dailogueTrigger = trigger;
        slideCount = -1;
        lineCount = 0;
        DialogueActive = true;
        _boxAccumulatedText.Clear();

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
        if (_typeLineCoroutine != null)
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

        if (_typeLineCoroutine != null) StopCoroutine(_typeLineCoroutine);
        _typeLineCoroutine = StartCoroutine(TypeLine());
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

    void ClearSlideTextBoxes(int slideIndex)
    {
        if (slideIndex < 0 || slideIndex >= dialogueSlides.Count) return;

        Dialogue slide = dialogueSlides[slideIndex];
        bool slideUsedUniversalBox = false;

        foreach (DialogueLine line in slide.Lines)
        {
            TMP_Text box = GetTargetTextBox(line);
            if (box == null) continue;
            box.text = "";
            _boxAccumulatedText[box] = "";
            if (box == dialogueText) slideUsedUniversalBox = true;
        }

        if (slide.alternitiveCanvasGroup != null)
        {
            slide.alternitiveCanvasGroup.alpha = 0f;
            slide.alternitiveCanvasGroup.interactable = false;
            slide.alternitiveCanvasGroup.blocksRaycasts = false;
        }

        if (slideUsedUniversalBox)
        {
            dialogueText.text = "";
            _boxAccumulatedText[dialogueText] = "";
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

    IEnumerator TypeLine()
    {
        Dialogue slide = dialogueSlides[slideCount];
        DialogueLine currentLine = slide.Lines[lineCount];
        CharacterDialogueInfo character = ResolveCharacter(slide, currentLine);

        ApplyCharacterToNameText(character);

        _currentLine = currentLine.Line;
        _currentTargetBox = GetTargetTextBox(currentLine);

        bool usingUniversalBox = currentLine.alternitiveTextBox == null;
        DialogueBox.SetActive(usingUniversalBox);

        _currentTargetBox.color = character != null ? character.textColor : defaultTextColor;

        if (character != null && character.fontAsset != null)
        {
            _currentTargetBox.font = character.fontAsset;
        }
        _currentTargetBox.fontSize = character != null ? character.fontSize : defaultFontSize;

        if (!_boxAccumulatedText.TryGetValue(_currentTargetBox, out _currentPrefix))
        {
            _currentPrefix = "";
        }

        if (!string.IsNullOrEmpty(_currentPrefix)) _currentPrefix += " ";

        float delay = character != null ? character.TextSpeed : defaultTextSpeed;
        string typingSound = character != null ? character.Voice : defaultTypingSound;

        if (InteractionMinigameManager.Instance != null)
        {
            InteractionMinigameManager.Instance.SetCharacterBody(currentLine.CharacterBody);
            InteractionMinigameManager.Instance.SetCharacterFace(currentLine.CharacterFace);
        }

        currentLine.LineStartEvent?.Invoke();

        string typedSoFar = "";
        foreach (char c in _currentLine)
        {
            if (ShowDebugLogs) Debug.Log("Typing");
            while (UILogic.GameIsPaused)
            {
                if (ShowDebugLogs) Debug.Log("Paused");
                yield return null;
            }
            typedSoFar += c;
            _currentTargetBox.text = _currentPrefix + typedSoFar;
            FModAudioManager.instance.PlaySoundByName(typingSound);
            yield return new WaitForSecondsRealtime(delay);
        }

        _typeLineCoroutine = null;
        _boxAccumulatedText[_currentTargetBox] = _currentPrefix + _currentLine;
        currentLine.LineEndEvent?.Invoke();

        if (_dailogueTrigger.EndOfLines.Length > 0 && IsLastLineOfLastSlide()) OpenChoiceBox();
    }

    void SkipToEndOfLine()
    {
        StopCoroutine(_typeLineCoroutine);
        _typeLineCoroutine = null;

        if (_currentTargetBox != null)
        {
            _currentTargetBox.text = _currentPrefix + _currentLine;
            _boxAccumulatedText[_currentTargetBox] = _currentPrefix + _currentLine;
        }

        DialogueLine currentLine = dialogueSlides[slideCount].Lines[lineCount];
        currentLine.LineEndEvent?.Invoke();

        if (_dailogueTrigger.EndOfLines.Length > 0 && IsLastLineOfLastSlide()) OpenChoiceBox();
    }

    public void OpenChoiceBox()
    {
        ChoiceBox.SetActive(true);
        ChoiceBoxIsOpen = true;
        int ChoiceIndex = 0;
        foreach (DialogueChoice choice in _dailogueTrigger.EndOfLines)
        {
            GameObject choiceInstance = Instantiate(Choice, ChoiceBox.transform);
            TextMeshProUGUI tmpText = choiceInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null) tmpText.text = choice.Text;

            DiologueChoiceWrapper ChoiceNumber = choiceInstance.GetComponentInChildren<DiologueChoiceWrapper>();
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
        DeactivateDialogueBox();
        _dailogueTrigger.EndDialogueDefultEvent();
        if (_dailogueTrigger.EndOfLines.Length > 0)
        {
            _dailogueTrigger.EndDialogue(CurrentChoiceIndex);
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
        _typeLineCoroutine = null;
        _boxAccumulatedText.Clear();
        dialogueText.text = "";
        nameText.text = "";

        ChoiceBox.SetActive(false);
        ChoiceBoxIsOpen = false;
        foreach (Transform child in ChoiceBox.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}