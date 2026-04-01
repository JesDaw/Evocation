using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public List<CharacterDialogueInfo> characterList; // whats this for?
    [Header("Text box referances")]
    [SerializeField] GameObject DialogueBox;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    [Header("Text box customisation")]
    public Color defaultTextColor = new Color(0.99f,0.99f,0.99f,1f);
    public Color defaultNameColor = new Color(0f,0f,0f,1f);
    [Header("Debug")]
    [SerializeField] bool ShowDebugLogs = false;

    

    List<Dialogue> dialogueSlides = new List<Dialogue>();
    Coroutine _typeLineCoroutine;
    string _currentLine;
    int slideCount = 0;
    DailogueTrigger _dailogueTrigger;
    public bool DialogueActive { get; private set; }
    void SubscribeToInputs()
    {
        if (GlobalInputManager.Instance == null) 
        {
            UnityEngine.Debug.LogWarning($"DialogueManager didnt find the GlobalInputManager");
            return;
        }
        var uiActions = GlobalInputManager.Instance.InputActions.UI;
        uiActions.ConfirmDialogue.performed += OnConfirmDialoguePressed;
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
        slideCount = 0;
        DialogueActive = true;

        DialogueBox.SetActive(true);
        
        GlobalInputManager.Instance.SetDialogueMode();
        
        DisplayNextSlide(); 
    }

    public void OnConfirmDialoguePressed(InputAction.CallbackContext context)
    {
        if (!context.performed || !DialogueActive || UILogic.GameIsPaused) return;
        
        if(ShowDebugLogs) UnityEngine.Debug.Log("Dialogue confirm button pressed");
        OnConfirmDialoguePressedLogic();
    }

    void OnConfirmDialoguePressedLogic()
    {
        if (_typeLineCoroutine != null) 
        {
            SkipToEndOfLine();
            return;
        }
        if (slideCount >= dialogueSlides.Count - 1)
        {
            EndDialogue();
            return;
        }


        slideCount++;
        DisplayNextSlide();
    }

    void DisplayNextSlide()
    {
        nameText.text = dialogueSlides[slideCount].CharacterName;
        _currentLine = dialogueSlides[slideCount].Line;

        if (_typeLineCoroutine != null)
            StopCoroutine(_typeLineCoroutine);
        _typeLineCoroutine = StartCoroutine(TypeLine(_currentLine, dialogueSlides[slideCount].DialogueDelaySeconds));
    }

    IEnumerator TypeLine(string line, float speed)
    {
        dialogueText.text = "";
        CharacterDialogueInfo speakingCharacter = FindCharacter(nameText.text);
        
        if (speakingCharacter != null)
        {
          nameText.color = speakingCharacter.nameColor; 
          dialogueText.color = speakingCharacter.textColor; 
        } else {
            nameText.color = defaultNameColor;
            dialogueText.color = defaultTextColor;
        }

        foreach (char c in line)
        {
            while (UILogic.GameIsPaused)
            {
                yield return null; 
            } 
            dialogueText.text += c;
            FModAudioManager.instance.PlaySoundByName("dialogueType");
            yield return new WaitForSecondsRealtime(speed);
        }
        _typeLineCoroutine = null;
        if (_dailogueTrigger.EndOfLines.Length > 1) OpenChoiceBox();

    }

    void SkipToEndOfLine()
    {
        StopCoroutine(_typeLineCoroutine);
        _typeLineCoroutine = null;
        dialogueText.text = _currentLine;
        if (_dailogueTrigger.EndOfLines.Length > 1) OpenChoiceBox();
    }

    public void OpenChoiceBox()
    {
        UnityEngine.Debug.Log("idk how the choice bax is going to work yet");
    }

    public void EndDialogue()
    {
        if(DialogueActive == false) return;
        DialogueActive = false;
        slideCount = 0;
        _typeLineCoroutine = null;
        DeactivateDialogueBox();
        dialogueText.text = "";
        nameText.text = "";
        _dailogueTrigger.EndDialogue(0);
        
        if (CameraControlSwitcher.Instance != null && CameraControlSwitcher.Instance.FreeCamIsActive)
        {
            GlobalInputManager.Instance.SetFreeCamMode();
        }
        else
        {
            GlobalInputManager.Instance.SetPlayerCharacterMode();
        }
    }

    public void DeactivateDialogueBox()
    {
        DialogueBox.SetActive(false);
    }

    private CharacterDialogueInfo FindCharacter(string name)
    {
        foreach (CharacterDialogueInfo character in characterList)
        {
            if (character.CharacterName == name)
            {
                return character;
            }
        }
        return null;
    }

}