using UnityEngine;

/// <summary>
/// Put on a rough sprite/plane.
///
/// Apparent-size lock: while on, whenever you change ONLY the object's depth (its Z, since the
/// camera looks down +Z), the object is rescaled AND its offset from the camera axis (its X/Y as
/// seen from the reference point) is scaled by the same ratio, so it keeps the same size and screen
/// position as seen from the reference point. In Edit mode, the reference point is the point inside
/// the camera bounds BoxCollider closest to the object (closest X, Y and Z). In Play mode, the
/// reference point is the live camera position.
///
/// How it stays undo-safe and stable: the lock keeps an "anchor" (the object's settled state
/// plus the camera pose it was measured from). A depth-only change is always computed from the
/// anchor, never incrementally. Any other change (you move X/Y, change scale yourself, undo,
/// redo, camera moves) simply re-anchors to the new state.
///
/// Sorting order: optionally set to -Z * sortingOrderPerUnit, every Update.
///
/// Bounds convention: the object's rectangle is the Renderer's localBounds (SpriteRenderer incl.
/// pivot and tiled/sliced size). If there is no usable renderer, proxySize is used. The rectangle
/// is treated as a plane parallel to the camera.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class EnvironmentArtAsset : MonoBehaviour
{
    [Tooltip("Found automatically (parent first, then anywhere in the scene). Assign manually to override.")]
    public EnvironmentArtSettings settings;

    [Tooltip("When on, changing only this object's Z keeps its on-screen size and position (as seen from the reference point).")]
    public bool lockApparentSize;

    [Tooltip("Local width/height at scale 1. Only used if there is no Renderer with bounds.")]
    public Vector2 proxySize = Vector2.one;

    [Header("Sorting")]
    [Tooltip("Keep the Renderer's sorting order equal to -Z * Sorting Order Per Unit.")]
    public bool autoSortingOrder = true;

    [Tooltip("1 means Z = 3000 gives order -3000. Sorting orders are limited to -32768..32767.")]
    public float sortingOrderPerUnit = 1f;

    // ---- Lock state. Not serialized: rebuilt on load/enable. Public only so the Inspector can show it. ----
    [System.NonSerialized] public bool hasAnchor;
    [System.NonSerialized] public Vector3 anchorCameraPosition;   // the reference point: closest point of bounds in edit mode, live camera in play mode
    [System.NonSerialized] public Quaternion anchorCameraRotation = Quaternion.identity;
    [System.NonSerialized] public float anchorFieldOfView = 40f;
    [System.NonSerialized] public float anchorDepth;
    [System.NonSerialized] public Vector3 anchorScale;
    [System.NonSerialized] public Vector2 anchorProjectedPixels;

    bool anchorUsedBounds;
    Bounds anchorBounds;
    Vector3 anchorLateral;     // world-space offset of the object from the camera axis, at anchor time
    bool hasLast;
    Vector3 lastPosition;      // state after our last write / last observed settled state
    Vector3 lastScale;
    Renderer cachedRenderer;
    readonly Vector3[] cornerBuffer = new Vector3[4];

    void OnEnable()
    {
        hasAnchor = false;
        hasLast = false;
        if (settings == null)
        {
            settings = GetComponentInParent<EnvironmentArtSettings>();
        }
        if (settings == null)
        {
            settings = FindFirstObjectByType<EnvironmentArtSettings>();
        }
    }

    void Reset()
    {
        settings = GetComponentInParent<EnvironmentArtSettings>();
        if (settings == null)
        {
            settings = FindFirstObjectByType<EnvironmentArtSettings>();
        }
    }

    void OnValidate()
    {
        proxySize.x = Mathf.Max(0.0001f, proxySize.x);
        proxySize.y = Mathf.Max(0.0001f, proxySize.y);
        sortingOrderPerUnit = Mathf.Max(0f, sortingOrderPerUnit);
    }

    void Update()
    {
        ApplySortingOrder();
        UpdateLock();
    }

    // ------------------------------------------------------------------ sorting

    public int GetExpectedSortingOrder()
    {
        return Mathf.Clamp(Mathf.RoundToInt(-transform.position.z * sortingOrderPerUnit), short.MinValue, short.MaxValue);
    }

    void ApplySortingOrder()
    {
        if (!autoSortingOrder)
        {
            return;
        }
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer == null)
            {
                return;
            }
        }

        int order = GetExpectedSortingOrder();
        if (cachedRenderer.sortingOrder == order)
        {
            return;
        }
        cachedRenderer.sortingOrder = order;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(cachedRenderer);
        }
#endif
    }

    // ------------------------------------------------------------------ lock

    void UpdateLock()
    {
        if (!lockApparentSize)
        {
            hasAnchor = false;
            hasLast = false;
            return;
        }
        if (settings == null)
        {
            settings = GetComponentInParent<EnvironmentArtSettings>();
            if (settings == null)
            {
                return;
            }
        }
        Camera cam = settings.ResolveCamera();
        if (cam == null || cam.orthographic)
        {
            return;
        }

        Vector3 pos = transform.position;
        Vector3 scale = transform.localScale;

        // First frame with the lock on: capture the current state as the baseline.
        if (!hasLast)
        {
            SetAnchor(pos, scale, cam);
            lastPosition = pos;
            lastScale = scale;
            hasLast = true;
            return;
        }

        // If the active reference changed (bounds edited in Edit mode, or live camera moved in Play mode),
        // re-anchor the settled state.
        if (ReferenceChanged(cam))
        {
            SetAnchor(lastPosition, lastScale, cam);
        }

        Vector3 delta = pos - lastPosition;
        bool posSame = delta.magnitude <= PositionTolerance(pos);
        bool scaleSame = Vector3.Distance(scale, lastScale) <= PositionTolerance(scale);
        if (posSame && scaleSame)
        {
            return; // nothing changed
        }

        // Is this a pure depth change (nothing perpendicular to the camera axis, scale untouched)?
        bool depthOnly = false;
        Vector3 forward = anchorCameraRotation * Vector3.forward;
        if (hasAnchor && scaleSame)
        {
            float along = Vector3.Dot(delta, forward);
            Vector3 perpendicular = delta - forward * along;
            depthOnly = perpendicular.magnitude <= PositionTolerance(pos) && Mathf.Abs(along) > 0f;
        }

        if (!depthOnly)
        {
            // You moved X/Y, rescaled, undid/redid, etc. Accept it as the new baseline.
            SetAnchor(pos, scale, cam);
            lastPosition = pos;
            lastScale = scale;
            return;
        }

        float newDepth = Vector3.Dot(pos - anchorCameraPosition, forward);
        if (newDepth <= EnvironmentArtSettings.NearEpsilon)
        {
            // Crossed the camera plane: leave the object alone; the Inspector explains why.
            hasAnchor = false;
            lastPosition = pos;
            lastScale = scale;
            return;
        }

        float ratio = newDepth / anchorDepth;
        transform.position = anchorCameraPosition + forward * newDepth + anchorLateral * ratio;
        transform.localScale = anchorScale * ratio;

        lastPosition = transform.position;
        lastScale = transform.localScale;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(transform);
        }
#endif
    }

    bool ReferenceChanged(Camera cam)
    {
        if (!hasAnchor)
        {
            return true;
        }
        if (Quaternion.Angle(cam.transform.rotation, anchorCameraRotation) > 0.001f)
        {
            return true;
        }
        if (!Application.isPlaying && settings.TryGetCameraBounds(out Bounds b))
        {
            return !anchorUsedBounds
                || Vector3.Distance(b.center, anchorBounds.center) > 1e-4f
                || Vector3.Distance(b.size, anchorBounds.size) > 1e-4f;
        }
        // In Play mode (or when no bounds are assigned): the reference is the live camera position.
        return anchorUsedBounds || Vector3.Distance(cam.transform.position, anchorCameraPosition) > PositionTolerance(cam.transform.position);
    }

    void SetAnchor(Vector3 position, Vector3 scale, Camera cam)
    {
        anchorCameraRotation = cam.transform.rotation;
        anchorFieldOfView = cam.fieldOfView;
        anchorUsedBounds = !Application.isPlaying && settings.TryGetCameraBounds(out anchorBounds);
        anchorCameraPosition = anchorUsedBounds ? anchorBounds.ClosestPoint(position) : cam.transform.position;

        Vector3 forward = anchorCameraRotation * Vector3.forward;
        Vector3 relative = position - anchorCameraPosition;
        float depth = Vector3.Dot(relative, forward);
        if (depth <= EnvironmentArtSettings.NearEpsilon)
        {
            hasAnchor = false;
            return;
        }

        anchorDepth = depth;
        anchorLateral = relative - forward * depth;
        anchorScale = scale;
        hasAnchor = true;

        GetWorldCornersAt(position, scale, cornerBuffer);
        if (!settings.TryProject(EnvironmentArtSettings.GetViewMatrix(anchorCameraPosition, anchorCameraRotation), anchorFieldOfView, cornerBuffer, out anchorProjectedPixels, out _))
        {
            anchorProjectedPixels = Vector2.zero;
        }
    }

    // Float tolerance that grows with magnitude (positions near Z = 3000 have ~0.0002 precision).
    static float PositionTolerance(Vector3 value)
    {
        return 1e-5f + 2e-6f * value.magnitude;
    }

    // ------------------------------------------------------------------ bounds

    public Bounds GetLocalBounds()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            Bounds b = r.localBounds;
            if (b.size.x > 0f && b.size.y > 0f)
            {
                return b;
            }
        }
        return new Bounds(Vector3.zero, new Vector3(proxySize.x, proxySize.y, 0f));
    }

    /// <summary>Fills 4 world-space corners of the object's current rectangle (array length must be 4).</summary>
    public void GetWorldCorners(Vector3[] corners)
    {
        Bounds b = GetLocalBounds();
        Vector3 c = b.center;
        Vector3 e = b.extents;
        corners[0] = transform.TransformPoint(new Vector3(c.x - e.x, c.y - e.y, c.z));
        corners[1] = transform.TransformPoint(new Vector3(c.x - e.x, c.y + e.y, c.z));
        corners[2] = transform.TransformPoint(new Vector3(c.x + e.x, c.y + e.y, c.z));
        corners[3] = transform.TransformPoint(new Vector3(c.x + e.x, c.y - e.y, c.z));
    }

    /// <summary>Same, but for a hypothetical world position and local scale (parent assumed unrotated/unskewed).</summary>
    void GetWorldCornersAt(Vector3 position, Vector3 localScale, Vector3[] corners)
    {
        Bounds b = GetLocalBounds();
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        Matrix4x4 m = Matrix4x4.TRS(position, transform.rotation, Vector3.Scale(localScale, parentScale));
        Vector3 c = b.center;
        Vector3 e = b.extents;
        corners[0] = m.MultiplyPoint3x4(new Vector3(c.x - e.x, c.y - e.y, c.z));
        corners[1] = m.MultiplyPoint3x4(new Vector3(c.x - e.x, c.y + e.y, c.z));
        corners[2] = m.MultiplyPoint3x4(new Vector3(c.x + e.x, c.y + e.y, c.z));
        corners[3] = m.MultiplyPoint3x4(new Vector3(c.x + e.x, c.y - e.y, c.z));
    }

    /// <summary>World size of the rectangle (width, height), ignoring rotation.</summary>
    public Vector2 GetWorldSize()
    {
        Bounds b = GetLocalBounds();
        Vector3 s = transform.lossyScale;
        return new Vector2(b.size.x * Mathf.Abs(s.x), b.size.y * Mathf.Abs(s.y));
    }
}