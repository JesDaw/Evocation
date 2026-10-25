using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : MonoBehaviour
{
    public CharacterData Character { get; private set; }
    
    [Tooltip("Auto-fills with the child 'Loadout slot' Image if left blank")]
    [SerializeField] Image headshotFrame;
    [SerializeField] bool debugLogs;

    void Awake()
    {
        // If headshotFrame isn't set, or was accidentally set to the outer frame itself,
        // automatically grab the leaf child's Image component!
        if ((headshotFrame == null || headshotFrame.transform == transform) && transform.childCount > 0)
        {
            headshotFrame = transform.GetChild(0).GetComponent<Image>();
        }
    }

    public void AssignCharacter(CharacterData newCharacter)
    {
        Character = newCharacter;
        if (debugLogs) Debug.Log($"{gameObject.name} assigned character: {(Character != null ? Character.characterName : "None")}");
        UpdateFrame();
    }

    public void UpdateCharacterDesplay()
    {
        if (Character != null)
            CharacterSelect.Instance.UpdateCurrentDesplayedCharacter(Character);
    }

    public void UpdateFrame()
    {
        if (headshotFrame == null) return;

        bool hasCharacter = Character != null;
        headshotFrame.enabled = hasCharacter;
        headshotFrame.sprite = hasCharacter ? Character.headshot : null;
    }

    public void OnClick()
    {
        if (Character == null) return;
        CharacterSelect.Instance.OnCharacterClicked(Character);
    }
}