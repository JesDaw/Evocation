using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a flame painted into a single image move by warping the image's mesh.
/// Everything below the rim stays still, and the movement grows toward the top of the image.
/// </summary>
[RequireComponent(typeof(Graphic))]
public class UIFlameFlicker : BaseMeshEffect
{
    const int Columns = 14;
    const int FlameRows = 14;

    [Tooltip("Height where the flame starts, as a fraction of the image. Nothing below this moves.")]
    [Range(0f, 1f)] [SerializeField] float rimHeight = 0.58f;

    [Header("Sway")]
    [Tooltip("Sideways movement at the tip, as a fraction of the image width.")]
    [Range(0f, 0.2f)] [SerializeField] float swayAmount = 0.035f;
    [Min(0.01f)] [SerializeField] float swaySeconds = 1.1f;
    [Tooltip("How many waves run up the flame at once.")]
    [Min(0f)] [SerializeField] float swayWaves = 1.5f;

    [Header("Height")]
    [Tooltip("How much the tip rises and falls, as a fraction of the image height.")]
    [Range(0f, 0.2f)] [SerializeField] float stretchAmount = 0.05f;
    [Min(0.01f)] [SerializeField] float stretchSeconds = 0.7f;

    [Header("Brightness")]
    [Tooltip("How much the top of the flame fades in and out.")]
    [Range(0f, 1f)] [SerializeField] float flickerAmount = 0.12f;
    [Min(0.01f)] [SerializeField] float flickerSeconds = 0.45f;

    readonly List<UIVertex> _stream = new List<UIVertex>();
    readonly UIVertex[] _quad = new UIVertex[4];
    float _seed;

    protected override void Awake()
    {
        base.Awake();
        // Keeps several flames from moving in step.
        _seed = Random.value * 10f;
    }

    void Update()
    {
        if (Time.timeScale > 0f) graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        vh.GetUIVertexStream(_stream);
        if (_stream.Count != 6) return; // Only a single quad can be turned into a grid.
        vh.Clear();

        // Quads are emitted as two triangles: 0,1,2 and 2,3,0.
        UIVertex bottomLeft = _stream[0];
        UIVertex topLeft = _stream[1];
        UIVertex topRight = _stream[2];
        UIVertex bottomRight = _stream[4];
        Rect rect = graphic.rectTransform.rect;

        // Row 0 is everything below the rim, which never moves. The rest divide up the flame.
        for (int row = 0; row <= FlameRows; row++)
        {
            float rowStart = row == 0 ? 0f : Mathf.Lerp(rimHeight, 1f, (row - 1f) / FlameRows);
            float rowEnd = Mathf.Lerp(rimHeight, 1f, (float)row / FlameRows);

            for (int column = 0; column < Columns; column++)
            {
                float columnStart = (float)column / Columns;
                float columnEnd = (float)(column + 1) / Columns;

                _quad[0] = GridVertex(bottomLeft, topLeft, topRight, bottomRight, columnStart, rowStart, rect);
                _quad[1] = GridVertex(bottomLeft, topLeft, topRight, bottomRight, columnStart, rowEnd, rect);
                _quad[2] = GridVertex(bottomLeft, topLeft, topRight, bottomRight, columnEnd, rowEnd, rect);
                _quad[3] = GridVertex(bottomLeft, topLeft, topRight, bottomRight, columnEnd, rowStart, rect);
                vh.AddUIVertexQuad(_quad);
            }
        }
    }

    UIVertex GridVertex(UIVertex bottomLeft, UIVertex topLeft, UIVertex topRight, UIVertex bottomRight, float x, float y, Rect rect)
    {
        UIVertex vertex = bottomLeft;
        Vector3 bottomPosition = Vector3.Lerp(bottomLeft.position, bottomRight.position, x);
        Vector3 topPosition = Vector3.Lerp(topLeft.position, topRight.position, x);
        vertex.position = Vector3.Lerp(bottomPosition, topPosition, y);
        vertex.uv0 = Vector4.Lerp(Vector4.Lerp(bottomLeft.uv0, bottomRight.uv0, x), Vector4.Lerp(topLeft.uv0, topRight.uv0, x), y);

        // Squared so the base of the flame stays attached to the rim.
        float flame = Mathf.Clamp01(Mathf.InverseLerp(rimHeight, 1f, y));
        flame *= flame;
        if (flame <= 0f) return vertex;

        float time = Time.time;
        float wave = time / swaySeconds + y * swayWaves + _seed;
        float sway = Mathf.Sin(wave * Mathf.PI * 2f) + 0.5f * Mathf.Sin(wave * 1.7f * Mathf.PI * 2f);
        float stretch = Mathf.PerlinNoise(time / stretchSeconds, _seed) * 2f - 1f;
        float flicker = Mathf.PerlinNoise(time / flickerSeconds, _seed + 5f);

        vertex.position.x += sway * swayAmount * flame * rect.width;
        vertex.position.y += stretch * stretchAmount * flame * rect.height;

        Color color = vertex.color;
        color.a *= 1f - flicker * flickerAmount * flame;
        vertex.color = color;
        return vertex;
    }
}
