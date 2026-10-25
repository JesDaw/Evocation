using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "DialoguePresentationStyle", menuName = "Dialogue/Presentation Style")]
public class DialoguePresentationStyle : ScriptableObject
{
    [Header("Sentence object")]
    [Tooltip("Optional prefab with a TextMeshProUGUI (for custom materials, outlines, etc). " +
        "Leave empty to create a plain text object from the settings below.")]
    public DialogueTextSurface sentencePrefab;
    [Tooltip("Width and height of each sentence, in canvas units.")]
    public Vector2 sentenceSize = new Vector2(900f, 200f);

    [Header("Typography")]
    [Tooltip("Leave empty to keep the speaker's font (CharacterDialogueInfo) or TMP's default.")]
    public TMP_FontAsset font;
    public float fontSize = 42f;
    public TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft;

    [Header("Reveal")]
    [Tooltip("Wrapped around every line; {0} is the line. Example: <+fade>{0}</+fade>")]
    public string lineFormat = "{0}";

    [Header("Sentence history")]
    [Tooltip("Opacity of earlier sentences while a newer one is current.")]
    [Range(0f, 1f)] public float previousSentenceOpacity = 0.35f;
    [Tooltip("Seconds to fade sentences between full and previous opacity, and out when removed.")]
    public float fadeDuration = 0.3f;
    [Tooltip("Oldest sentences are removed beyond this many. 0 = keep all until the slide ends.")]
    public int maxVisibleSentences = 0;

    public string FormatLine(string line)
    {
        if (string.IsNullOrEmpty(lineFormat) || !lineFormat.Contains("{0}")) return line;
        return lineFormat.Replace("{0}", line);
    }

    // Font size, alignment and box size; colour and speaker font stay with CharacterDialogueInfo.
    public void ApplyTo(TMP_Text text)
    {
        if (font != null) text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.rectTransform.sizeDelta = sentenceSize;
    }
}
