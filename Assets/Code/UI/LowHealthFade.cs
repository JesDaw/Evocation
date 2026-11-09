using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sits on top of a black screen edge fade and replaces it with a colored one as the active player's health gets low.
/// The fade sprites are pure black, so tinting them can't change their color. This draws the same gradient instead.
/// </summary>
public class LowHealthFade : MaskableGraphic
{
    [Tooltip("The black fade this one replaces.")]
    [SerializeField] Graphic blackFade;
    [Tooltip("True if the solid edge of the gradient is at the top of the rect, false if it is at the bottom.")]
    [SerializeField] bool solidAtTop;
    [Tooltip("Part of the rect on the clear side that stays empty, matching the empty space in the black sprite.")]
    [Range(0f, 1f)] [SerializeField] float clearMargin = 0.055f;

    [Header("Health")]
    [Tooltip("The color starts to appear below this fraction of health.")]
    [Range(0f, 1f)] [SerializeField] float startBelow = 0.4f;
    [Tooltip("The color is fully showing at or below this fraction of health.")]
    [Range(0f, 1f)] [SerializeField] float fullBelow = 0.25f;
    [Tooltip("How long the color takes to fade fully in or out.")]
    [Min(0.01f)] [SerializeField] float fadeSeconds = 0.25f;

    [Header("Pulse")]
    [Tooltip("How far the color dims at the low point of each pulse. 0 turns the pulse off.")]
    [Range(0f, 1f)] [SerializeField] float pulseStrength = 0.3f;
    [Tooltip("Time for one full pulse.")]
    [Min(0.01f)] [SerializeField] float pulseSeconds = 1.4f;

    GameObject _player;
    Stats _playerStats;
    float _amount;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Jumps straight to the right value, because this can be drawn before its first Update.
        _amount = TargetAmount();
        ApplyAmount();
    }

    void Update()
    {
        _amount = Mathf.MoveTowards(_amount, TargetAmount(), Time.unscaledDeltaTime / fadeSeconds);
        ApplyAmount();
    }

    // Reads the player's real health, because the health bar briefly empties while there is no player.
    float TargetAmount()
    {
        GameObject player = ActivePlayer.Instance != null ? ActivePlayer.Instance.CurrentPlayer : null;
        if (player != _player)
        {
            _player = player;
            _playerStats = player != null ? player.GetComponent<Stats>() : null;
        }

        if (_playerStats == null || _playerStats._MaxHealth <= 0) return 0f;
        return Mathf.InverseLerp(startBelow, fullBelow, _playerStats._CurrentHealth / _playerStats._MaxHealth);
    }

    void ApplyAmount()
    {
        float pulse = (Mathf.Cos(Time.time / pulseSeconds * Mathf.PI * 2f) + 1f) * 0.5f;
        canvasRenderer.SetAlpha(_amount * Mathf.Lerp(1f - pulseStrength, 1f, pulse));
        if (blackFade != null) blackFade.canvasRenderer.SetAlpha(1f - _amount);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        Color32 solid = color;
        Color32 clear = solid;
        clear.a = 0;

        float solidY = solidAtTop ? rect.yMax : rect.yMin;
        float clearY = Mathf.Lerp(solidAtTop ? rect.yMin : rect.yMax, solidY, clearMargin);

        vh.AddVert(new Vector3(rect.xMin, solidY), solid, Vector4.zero);
        vh.AddVert(new Vector3(rect.xMax, solidY), solid, Vector4.zero);
        vh.AddVert(new Vector3(rect.xMax, clearY), clear, Vector4.zero);
        vh.AddVert(new Vector3(rect.xMin, clearY), clear, Vector4.zero);
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}
