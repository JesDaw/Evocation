using System.Collections.Generic;
using UnityEngine;

public enum EnvironmentAssetRole
{
    PaintedLayer,
    PositionableGroup,
    ReusableProp,
    GroundOrSpriteShape
}

public struct EnvironmentAssetSpec
{
    public bool isValid;
    public string error;

    public Vector2 worldSize;
    public float depthAtClosestCamera;
    public float depthAtFarthestCamera;

    public Vector3 closestCameraPose;
    public Vector3 farthestCameraPose;

    public Vector2 maxProjectedPixels;
    public Vector2 minProjectedPixels;
    public Vector2 liveCameraProjectedPixels;
    public bool hasLiveCameraProjection;

    public Vector2Int recommendedSourcePixels;
    public float recommendedPPU;

    public bool hasCurrentSprite;
    public Vector2Int currentSpritePixels;
    public float currentSpritePPU;
    public bool isUndersized;
    public bool exceedsMaxTextureSize;

    public string warnings;
}

[ExecuteAlways]
[DisallowMultipleComponent]
public class EnvironmentArtAsset : MonoBehaviour
{
    [Tooltip("Found automatically (parent first, then anywhere in the scene). Assign manually to override.")]
    public EnvironmentArtSettings settings;

    [Header("Asset Classification")]
    [Tooltip("How the artist should treat this object in Procreate / Unity.")]
    public EnvironmentAssetRole role = EnvironmentAssetRole.PaintedLayer;

    [Tooltip("Optional notes exported to the artist specification sheet.")]
    public string artistNotes = "";

    [Header("Apparent-Size Lock")]
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
    [System.NonSerialized] public Vector3 anchorCameraPosition;
    [System.NonSerialized] public Quaternion anchorCameraRotation = Quaternion.identity;
    [System.NonSerialized] public float anchorFieldOfView = 40f;
    [System.NonSerialized] public float anchorDepth;
    [System.NonSerialized] public Vector3 anchorScale;
    [System.NonSerialized] public Vector2 anchorProjectedPixels;

    bool anchorUsedBounds;
    Bounds anchorBounds;
    Vector3 anchorLateral;
    bool hasLast;
    Vector3 lastPosition;
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

        if (!hasLast)
        {
            SetAnchor(pos, scale, cam);
            lastPosition = pos;
            lastScale = scale;
            hasLast = true;
            return;
        }

        if (ReferenceChanged(cam))
        {
            SetAnchor(lastPosition, lastScale, cam);
        }

        Vector3 delta = pos - lastPosition;
        bool posSame = delta.magnitude <= PositionTolerance(pos);
        bool scaleSame = Vector3.Distance(scale, lastScale) <= PositionTolerance(scale);
        if (posSame && scaleSame)
        {
            return;
        }

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
            SetAnchor(pos, scale, cam);
            lastPosition = pos;
            lastScale = scale;
            return;
        }

        float newDepth = Vector3.Dot(pos - anchorCameraPosition, forward);
        if (newDepth <= EnvironmentArtSettings.NearEpsilon)
        {
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

    static float PositionTolerance(Vector3 value)
    {
        return 1e-5f + 2e-6f * value.magnitude;
    }

    // ------------------------------------------------------------------ bounds & specs

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

    public Vector2 GetWorldSize()
    {
        Bounds b = GetLocalBounds();
        Vector3 s = transform.lossyScale;
        return new Vector2(b.size.x * Mathf.Abs(s.x), b.size.y * Mathf.Abs(s.y));
    }

    /// <summary>
    /// Computes the full artist specification (closest/farthest camera poses, max projected pixels,
    /// recommended canvas size, recommended PPU at unit scale, and warnings).
    /// </summary>
    public EnvironmentAssetSpec CalculateSpec()
    {
        EnvironmentAssetSpec spec = new EnvironmentAssetSpec();
        if (settings == null)
        {
            spec.isValid = false;
            spec.error = "Missing EnvironmentArtSettings.";
            return spec;
        }

        if (!settings.Validate(out string err))
        {
            spec.isValid = false;
            spec.error = err;
            return spec;
        }

        Camera cam = settings.ResolveCamera();
        Quaternion camRot = cam.transform.rotation;
        Vector3 forward = camRot * Vector3.forward;
        float fov = cam.fieldOfView;

        GetWorldCorners(cornerBuffer);
        spec.worldSize = GetWorldSize();
        // ... (rest of the method remains identical)

        // Determine closest and farthest camera poses to this object.
        if (settings.TryGetCameraBounds(out Bounds camBounds))
        {
            spec.closestCameraPose = camBounds.ClosestPoint(transform.position);
            spec.farthestCameraPose = new Vector3(
                Mathf.Clamp(transform.position.x, camBounds.min.x, camBounds.max.x),
                Mathf.Clamp(transform.position.y, camBounds.min.y, camBounds.max.y),
                camBounds.min.z
            );
        }
        else
        {
            spec.closestCameraPose = cam.transform.position;
            spec.farthestCameraPose = cam.transform.position;
        }

        spec.depthAtClosestCamera = Vector3.Dot(transform.position - spec.closestCameraPose, forward);
        spec.depthAtFarthestCamera = Vector3.Dot(transform.position - spec.farthestCameraPose, forward);

        if (spec.depthAtClosestCamera <= EnvironmentArtSettings.NearEpsilon)
        {
            spec.isValid = false;
            spec.error = "Object is inside or behind the closest camera plane.";
            return spec;
        }

        Matrix4x4 closestView = EnvironmentArtSettings.GetViewMatrix(spec.closestCameraPose, camRot);
        if (!settings.TryProject(closestView, fov, cornerBuffer, out spec.maxProjectedPixels, out string projErr))
        {
            spec.isValid = false;
            spec.error = projErr;
            return spec;
        }

        Matrix4x4 farthestView = EnvironmentArtSettings.GetViewMatrix(spec.farthestCameraPose, camRot);
        if (!settings.TryProject(farthestView, fov, cornerBuffer, out spec.minProjectedPixels, out _))
        {
            spec.minProjectedPixels = spec.maxProjectedPixels;
        }

        spec.hasLiveCameraProjection = settings.TryProject(cam.worldToCameraMatrix, fov, cornerBuffer, out spec.liveCameraProjectedPixels, out _);

        // Recommended source pixels = max projected pixels * quality multiplier
        float mult = Mathf.Max(0.1f, settings.pixelDensityMultiplier);
        int recW = Mathf.Max(1, Mathf.CeilToInt(spec.maxProjectedPixels.x * mult));
        int recH = Mathf.Max(1, Mathf.CeilToInt(spec.maxProjectedPixels.y * mult));
        spec.recommendedSourcePixels = new Vector2Int(recW, recH);

        // Recommended PPU so that replacing the sprite and resetting scale to (1,1,1) keeps exact worldSize
        spec.recommendedPPU = spec.worldSize.x > 0.0001f
            ? recW / spec.worldSize.x
            : 100f;

        List<string> warningList = new List<string>();

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            spec.hasCurrentSprite = true;
            Rect rect = sr.sprite.rect;
            spec.currentSpritePixels = new Vector2Int(Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height));
            spec.currentSpritePPU = sr.sprite.pixelsPerUnit;

            if (spec.currentSpritePixels.x < spec.recommendedSourcePixels.x * 0.95f ||
                spec.currentSpritePixels.y < spec.recommendedSourcePixels.y * 0.95f)
            {
                spec.isUndersized = true;
                warningList.Add($"Undersized sprite ({spec.currentSpritePixels.x}x{spec.currentSpritePixels.y} px < recommended {recW}x{recH} px)");
            }
        }
        else
        {
            warningList.Add("No Sprite assigned (using proxy/renderer bounds)");
        }

        if (recW > settings.maxTextureSizeWarning || recH > settings.maxTextureSizeWarning)
        {
            spec.exceedsMaxTextureSize = true;
            warningList.Add($"Exceeds max texture size threshold ({settings.maxTextureSizeWarning} px)");
        }

        spec.warnings = warningList.Count > 0 ? string.Join("; ", warningList) : "OK";
        spec.isValid = true;
        return spec;
    }
}