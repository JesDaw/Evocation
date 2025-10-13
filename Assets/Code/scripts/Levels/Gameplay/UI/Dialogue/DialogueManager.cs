using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public float textSpeed;
    //public Animator animator;
    [SerializeField] GameObject DialogueBox;

    Coroutine _typeLineCorutine;
    string _currentLine;
    bool _dialogueActive = false;
    public bool DialogueActive { get { return _dialogueActive; } set { _dialogueActive = value; } }
    DailogueTrigger _dailogueTrigger;
    bool _firstLine = true; //this is so cringe im sorry


    Queue<string> Lines;
    Queue<string> Names;
    Queue<float> TextSpeeds;

    void Start()
    {
        Lines = new Queue<string>();
        Names = new Queue<string>();
        TextSpeeds = new Queue<float>();
        
    }

    public void StartDialogue(DialogueSO dialogue, DailogueTrigger dailogueTrigger)
    {
        _dailogueTrigger = dailogueTrigger;
        ActivateDialogueBox();
        nameText.text = dialogue.CharacterName;
        Lines.Clear();
        foreach (string line in dialogue.Lines)
        {
            Lines.Enqueue(line);
        }
        DesplayNextSentance();
        
    }
    public void ActivateDialogueBox()
    {
        DialogueBox.SetActive(true);
    }

    public void OnConfirmDialoguePressed(InputAction.CallbackContext context)
    {
        if (!context.started || !DialogueActive) return;
        if (_firstLine)
        {
            _firstLine = false;
            return;
        }
        OnConfirmDialoguePressedLogic();
        
    }
    
    public void OnConfirmDialoguePressedLogic()
    {
        if (_typeLineCorutine == null && Lines.Count == 0)
        {
            _dailogueTrigger.EndDialogue();
            return;
        }
        if (_typeLineCorutine != null)
        {
            SkipToEndOfLine();
        }
        else
        {
            DesplayNextSentance();
        }
    }

    public void DesplayNextSentance()
    {
        _currentLine = Lines.Dequeue();
        _typeLineCorutine = StartCoroutine(TypeLine(_currentLine));        
    }

    IEnumerator TypeLine(string line)
    {
        dialogueText.text = "";
        foreach (char c in line.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
         _typeLineCorutine = null;
    }

    void SkipToEndOfLine()
    {
        StopCoroutine(_typeLineCorutine);
        _typeLineCorutine = null;
        dialogueText.text = _currentLine;
    }

    public void EndOfConvo()
    {
        _firstLine = true;
        DeactivateDialogueBox();
    }
    public void DeactivateDialogueBox()
    {
        DialogueBox.SetActive(false);
    }
 
}
