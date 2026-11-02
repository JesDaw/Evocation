using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a hollow outline that fills its RectTransform, at any thickness.
/// With no corner radius it is as round as the rect allows, so a square rect gives a circle.
/// </summary>
public class UIRing : MaskableGraphic
{
    const int SegmentsPerCorner = 18;

    [Min(0f)] [SerializeField] float thickness = 2.5f;
    [Tooltip("Width of the soft edge on each side, which keeps the ring from looking jagged.")]
    [Min(0f)] [SerializeField] float softEdge = 1f;
    [Tooltip("Roundness of the corners. 0 makes it as round as the rect allows.")]
    [Min(0f)] [SerializeField] float cornerRadius = 0f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float radius = cornerRadius > 0f ? Mathf.Min(cornerRadius, maxRadius) : maxRadius;

        Color32 solid = color;
        Color32 clear = solid;
        clear.a = 0;

        // Four outlines of vertices, by distance in from the outer edge: clear, solid, solid, clear.
        float[] insets = { thickness + softEdge, thickness, softEdge, 0f };
        Color32[] colors = { clear, solid, solid, clear };

        Vector2[] cornerCenters =
        {
            new Vector2(rect.xMax - radius, rect.yMax - radius),
            new Vector2(rect.xMin + radius, rect.yMax - radius),
            new Vector2(rect.xMin + radius, rect.yMin + radius),
            new Vector2(rect.xMax - radius, rect.yMin + radius),
        };

        for (int corner = 0; corner < cornerCenters.Length; corner++)
        {
            for (int segment = 0; segment <= SegmentsPerCorner; segment++)
            {
                float angle = (corner + (float)segment / SegmentsPerCorner) * Mathf.PI * 0.5f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                for (int i = 0; i < insets.Length; i++)
                    vh.AddVert(cornerCenters[corner] + direction * Mathf.Max(0f, radius - insets[i]), colors[i], Vector4.zero);
            }
        }

        int points = cornerCenters.Length * (SegmentsPerCorner + 1);
        for (int point = 0; point < points; point++)
        {
            int current = point * insets.Length;
            int next = (point + 1) % points * insets.Length;
            for (int i = 0; i < insets.Length - 1; i++)
            {
                vh.AddTriangle(current + i, next + i, next + i + 1);
                vh.AddTriangle(next + i + 1, current + i + 1, current + i);
            }
        }
    }
}
