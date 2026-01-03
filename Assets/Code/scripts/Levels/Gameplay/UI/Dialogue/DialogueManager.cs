using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    // UILogic uILogic;
    [SerializeField] GameObject DialogueBox;

    List<Dialogue> dialogueSlides = new List<Dialogue>();
    Coroutine _typeLineCoroutine;
    string _currentLine;
    int slideCount = 0;
    DailogueTrigger _dailogueTrigger;

    public bool DialogueActive { get; private set; }

    public void StartDialogue(List<Dialogue> slides, DailogueTrigger trigger)
    {
        dialogueSlides = slides;
        _dailogueTrigger = trigger;

        slideCount = 0;
        DialogueActive = true;

        DialogueBox.SetActive(true);
        DisplayNextSlide(); 
    }

    public void OnConfirmDialoguePressed(InputAction.CallbackContext context)
    {
        if (!context.started || !DialogueActive || UILogic.GameIsPaused) return;
        //UnityEngine.Debug.Log("OnConfirmDialoguePressed pressed");
        OnConfirmDialoguePressedLogic();
    }

    void OnConfirmDialoguePressedLogic()
    {
        if (_typeLineCoroutine != null) // still typing → skip
        {
            SkipToEndOfLine();
            return;
        }

        // Finished all lines?
        if (slideCount >= dialogueSlides.Count - 1)
        {
            EndDialogue();
            return;
        }

        // Go to next slide properly
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
        foreach (char c in line)
        {
            while (UILogic.GameIsPaused)
            {
                yield return null; 
            } 
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(speed);
        }
        _typeLineCoroutine = null;
    }

    void SkipToEndOfLine()
    {
        StopCoroutine(_typeLineCoroutine);
        _typeLineCoroutine = null;
        dialogueText.text = _currentLine;
    }

    void EndDialogue()
    {
        DialogueActive = false;
        slideCount = 0;
        _typeLineCoroutine = null;
        _dailogueTrigger.EndDialogue();
    }

    public void DeactivateDialogueBox()
    {
        DialogueBox.SetActive(false);
    }
}
