using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows the icon of the interactable the active player is in range of, while in free cam.
/// The camera can be far from the player then, so the icon in the world may be off screen.
/// </summary>
public class InteractionIconDisplay : MonoBehaviour
{
    public static InteractionIconDisplay Instance { get; private set; }

    [SerializeField] GameObject content;
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text promptText;

    GameObject _currentIcon;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        bool shouldShow = _currentIcon != null && CameraControlSwitcher.Instance != null && CameraControlSwitcher.Instance.FreeCamIsActive;
        if (content.activeSelf != shouldShow) content.SetActive(shouldShow);
    }

    public void Show(GameObject icon)
    {
        if (_currentIcon == icon) return;
        _currentIcon = icon;

        SpriteRenderer iconSprite = icon.GetComponentInChildren<SpriteRenderer>(true);
        iconImage.enabled = iconSprite != null;
        if (iconSprite != null)
        {
            iconImage.sprite = iconSprite.sprite;
            iconImage.color = iconSprite.color;
        }

        TMP_Text iconPrompt = icon.GetComponentInChildren<TMP_Text>(true);
        promptText.text = iconPrompt != null ? iconPrompt.text : "";
    }

    // Takes the icon because every out of range interactable hides its icon each frame,
    // which must not clear the icon of one that is in range.
    public void Hide(GameObject icon)
    {
        if (_currentIcon == icon) _currentIcon = null;
    }
}
