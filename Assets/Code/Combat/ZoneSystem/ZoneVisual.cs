using UnityEngine;

public class ZoneVisual : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] SpriteRenderer ringRenderer;
    [SerializeField] SpriteRenderer fillRenderer;

    [Header("Sizing")]
    [Tooltip("If true, the diameter at scale (1,1,1) is measured from the ring/fill renderers automatically. " +
             "Turn off to use Base Diameter below.")]
    [SerializeField] bool autoMeasure = true;
    [Tooltip("World-space diameter of the visual when the root scale is (1,1,1). Only used when Auto Measure is off.")]
    [SerializeField] float baseDiameter = 1f;

    public void Setup(AreaEffectData data)
    {
        if (data == null) return;

        ApplySize(data);
        SetRingColor(data.ringColor);
        SetFillColor(data.fillColor);
    }

    float MeasureBaseDiameter()
    {
        Vector3 original = transform.localScale;
        transform.localScale = Vector3.one;

        SpriteRenderer r = ringRenderer != null ? ringRenderer : fillRenderer;
        float measured = r != null ? r.bounds.size.x : baseDiameter;

        transform.localScale = original;
        return measured;
    }

    void ApplySize(AreaEffectData data)
    {
        float safeBase = Mathf.Max(0.0001f, autoMeasure ? MeasureBaseDiameter() : baseDiameter);

        if (data.shape == ZoneShape.Circle)
        {
            float s = (data.circleRadius * 2f) / safeBase;
            transform.localScale = new Vector3(s, s, s);
        }
        else
        {
            transform.localScale = new Vector3(data.boxSize.x / safeBase, data.boxSize.y / safeBase, 1f);
        }
    }

    public void SetRingColor(Color color)
    {
        if (ringRenderer != null) ringRenderer.color = color;
    }

    public void SetFillColor(Color color)
    {
        if (fillRenderer != null) fillRenderer.color = color;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}