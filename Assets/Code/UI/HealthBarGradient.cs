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
    [Min(0.01f)] [SerializeField] float rippleSeconds = 4f;
    [Tooltip("Length of one ripple, as a fraction of the bar.")]
    [Min(0.01f)] [SerializeField] float rippleLength = 0.6f;
    [Tooltip("How far the ripple shifts the color along the health scale.")]
    [Range(0f, 0.5f)] [SerializeField] float rippleColorShift = 0.05f;
    [Tooltip("How much the ripple changes brightness, so it still shows when the bar is one solid color.")]
    [Range(0f, 0.5f)] [SerializeField] float rippleBrightness = 0.06f;

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

                _quad[0] = Tint(LerpVertex(bottomLeft, bottomRight, start), rect, fill);
                _quad[1] = Tint(LerpVertex(topLeft, topRight, start), rect, fill);
                _quad[2] = Tint(LerpVertex(topLeft, topRight, end), rect, fill);
                _quad[3] = Tint(LerpVertex(bottomLeft, bottomRight, end), rect, fill);
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

    UIVertex Tint(UIVertex vertex, Rect rect, float fill)
    {
        float position = Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x);
        float ripple = Mathf.Sin((position / rippleLength - Time.time / rippleSeconds) * Mathf.PI * 2f);

        float scale = fill + colorSpread * Mathf.Max(0f, fill - position) + rippleColorShift * ripple;
        Color healthColor = EvaluateHealthColor(scale);

        float brightness = Mathf.Lerp(leftBrightness, 1f, position) * (1f + rippleBrightness * ripple);
        Color color = new Color(healthColor.r * brightness, healthColor.g * brightness, healthColor.b * brightness);

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
