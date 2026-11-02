using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Colors a health bar by how full it is (green, then yellow, then red). The color blends along the bar,
/// with the leading edge showing current health, and a slow ripple moves through the blend.
/// The graphic needs a material that uses vertex colors, such as the default UI material.
/// </summary>
[RequireComponent(typeof(Graphic))]
public class HealthBarGradient : BaseMeshEffect
{
    // Colors vary along the bar through vertex colors, so wide quads are split into columns this wide.
    const float ColumnWidth = 16f;

    [Tooltip("The image whose fill amount represents current health.")]
    [SerializeField] Image fillImage;

    [Header("Health Colors")]
    [SerializeField] Color healthyColor = new Color(0.18f, 0.88f, 0.32f);
    [SerializeField] Color warningColor = new Color(1f, 0.82f, 0.1f);
    [SerializeField] Color criticalColor = new Color(0.95f, 0.1f, 0.12f);
    [Tooltip("The bar stays fully the healthy color at or above this fraction of health.")]
    [Range(0f, 1f)] [SerializeField] float healthyAbove = 0.8f;
    [Tooltip("The bar is fully the critical color at or below this fraction of health.")]
    [Range(0f, 1f)] [SerializeField] float criticalBelow = 0.3f;

    [Header("Gradient")]
    [Tooltip("How far the left end of the bar runs ahead of current health on the color scale. 0 makes the whole bar one color.")]
    [Range(0f, 1f)] [SerializeField] float colorSpread = 0.2f;
    [Tooltip("Brightness at the left end of the bar. The right end is always full brightness.")]
    [Range(0f, 1f)] [SerializeField] float leftBrightness = 0.9f;

    [Header("Ripple")]
    [Min(0.01f)] [SerializeField] float rippleSeconds = 3f;
    [Tooltip("Length of one ripple, as a fraction of the bar.")]
    [Min(0.01f)] [SerializeField] float rippleLength = 0.6f;
    [Tooltip("How far the ripple shifts the color along the health scale. This is what shows between green and red.")]
    [Range(0f, 0.5f)] [SerializeField] float rippleColorShift = 0.05f;
    [Tooltip("How much the ripple changes brightness.")]
    [Range(0f, 0.5f)] [SerializeField] float rippleBrightness = 0.06f;
    [Tooltip("Ripple color while the bar is solid healthy color.")]
    [SerializeField] Color rippleHealthyColor = new Color(0.05f, 0.75f, 0.58f);
    [Tooltip("Ripple color while the bar is solid critical color.")]
    [SerializeField] Color rippleCriticalColor = new Color(0.85f, 0.08f, 0.38f);
    [Tooltip("How far past Healthy Above and Critical Below the ripple colors take to fade out, as a fraction of health.")]
    [Min(0.01f)] [SerializeField] float rippleColorFade = 0.1f;
    [Tooltip("How strongly the ripple colors show at the peak of each ripple.")]
    [Range(0f, 1f)] [SerializeField] float rippleStrength = 0.6f;

    readonly List<UIVertex> _stream = new List<UIVertex>();
    readonly UIVertex[] _quad = new UIVertex[4];
    float _lastFill = -1f;

    void Update()
    {
        float fill = CurrentFill();
        if (Time.timeScale > 0f || !Mathf.Approximately(fill, _lastFill))
        {
            _lastFill = fill;
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        Rect rect = graphic.rectTransform.rect;
        float fill = CurrentFill();
        Color rippleColor = EvaluateRippleColor(fill, out float rippleAmount);

        vh.GetUIVertexStream(_stream);
        vh.Clear();

        // Sliced images emit each quad as two triangles: 0,1,2 and 2,3,0.
        for (int i = 0; i + 5 < _stream.Count; i += 6)
        {
            UIVertex bottomLeft = _stream[i];
            UIVertex topLeft = _stream[i + 1];
            UIVertex topRight = _stream[i + 2];
            UIVertex bottomRight = _stream[i + 4];

            float width = bottomRight.position.x - bottomLeft.position.x;
            int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(width) / ColumnWidth));

            for (int column = 0; column < columns; column++)
            {
                float start = (float)column / columns;
                float end = (float)(column + 1) / columns;

                _quad[0] = Tint(LerpVertex(bottomLeft, bottomRight, start), rect, fill, rippleColor, rippleAmount);
                _quad[1] = Tint(LerpVertex(topLeft, topRight, start), rect, fill, rippleColor, rippleAmount);
                _quad[2] = Tint(LerpVertex(topLeft, topRight, end), rect, fill, rippleColor, rippleAmount);
                _quad[3] = Tint(LerpVertex(bottomLeft, bottomRight, end), rect, fill, rippleColor, rippleAmount);
                vh.AddUIVertexQuad(_quad);
            }
        }
    }

    float CurrentFill()
    {
        return fillImage != null ? fillImage.fillAmount : 1f;
    }

    Color EvaluateHealthColor(float health)
    {
        float t = Mathf.InverseLerp(criticalBelow, healthyAbove, health);
        return t >= 0.5f
            ? Color.Lerp(warningColor, healthyColor, (t - 0.5f) * 2f)
            : Color.Lerp(criticalColor, warningColor, t * 2f);
    }

    // The ripple colors only show near solid green or solid red, so they never mix with each other.
    Color EvaluateRippleColor(float health, out float amount)
    {
        float healthy = Mathf.InverseLerp(healthyAbove - rippleColorFade, healthyAbove, health);
        float critical = Mathf.InverseLerp(criticalBelow + rippleColorFade, criticalBelow, health);
        amount = Mathf.Max(healthy, critical);
        return healthy > critical ? rippleHealthyColor : rippleCriticalColor;
    }

    UIVertex Tint(UIVertex vertex, Rect rect, float fill, Color rippleColor, float rippleAmount)
    {
        float position = Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x);
        float wave = Mathf.Sin((position / rippleLength - Time.time / rippleSeconds) * Mathf.PI * 2f);
        float ripple = Mathf.SmoothStep(0f, 1f, (wave + 1f) * 0.5f);

        Color healthColor = EvaluateHealthColor(fill + colorSpread * Mathf.Max(0f, fill - position) + rippleColorShift * wave);
        float brightness = Mathf.Lerp(leftBrightness, 1f, position) * (1f + rippleBrightness * wave);
        Color color = new Color(healthColor.r * brightness, healthColor.g * brightness, healthColor.b * brightness);
        color = Color.Lerp(color, rippleColor, ripple * rippleStrength * rippleAmount);

        vertex.color = (Color)vertex.color * color;
        return vertex;
    }

    static UIVertex LerpVertex(UIVertex from, UIVertex to, float t)
    {
        UIVertex vertex = from;
        vertex.position = Vector3.Lerp(from.position, to.position, t);
        vertex.uv0 = Vector4.Lerp(from.uv0, to.uv0, t);
        return vertex;
    }
}
