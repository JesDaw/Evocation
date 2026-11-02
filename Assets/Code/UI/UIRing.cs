using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a hollow circle that fills its RectTransform, at any thickness.
/// </summary>
public class UIRing : MaskableGraphic
{
    const int Segments = 72;

    [Min(0f)] [SerializeField] float thickness = 2.5f;
    [Tooltip("Width of the soft edge on each side, which keeps the ring from looking jagged.")]
    [Min(0f)] [SerializeField] float softEdge = 1f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        float outer = Mathf.Min(rect.width, rect.height) * 0.5f;
        float inner = Mathf.Max(0f, outer - thickness);

        Color32 solid = color;
        Color32 clear = solid;
        clear.a = 0;

        // Four circles of vertices: clear, solid, solid, clear, from the inside out.
        float[] radii = { Mathf.Max(0f, inner - softEdge), inner, outer - softEdge, outer };
        Color32[] colors = { clear, solid, solid, clear };

        for (int segment = 0; segment <= Segments; segment++)
        {
            float angle = segment * Mathf.PI * 2f / Segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            for (int i = 0; i < radii.Length; i++)
                vh.AddVert(center + direction * radii[i], colors[i], Vector4.zero);
        }

        for (int segment = 0; segment < Segments; segment++)
        {
            int current = segment * radii.Length;
            int next = current + radii.Length;
            for (int i = 0; i < radii.Length - 1; i++)
            {
                vh.AddTriangle(current + i, next + i, next + i + 1);
                vh.AddTriangle(next + i + 1, current + i + 1, current + i);
            }
        }
    }
}
