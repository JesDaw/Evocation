using UnityEngine;

/// <summary>
/// One per scene. Holds the output camera and the reference resolution, plus the
/// projection helpers shared by assets and the Inspectors.
/// </summary>
public class EnvironmentArtSettings : MonoBehaviour
{
    [Tooltip("The Unity Camera that actually renders the scene (the one with the CinemachineBrain), NOT the CinemachineCamera. Falls back to Camera.main if empty.")]
    public Camera outputCamera;

    [Tooltip("Reference output resolution. Independent of the Game view window size.")]
    public Vector2Int referenceResolution = new Vector2Int(1920, 1080);

    [Tooltip("Your camera confiner BoxCollider. In Edit mode, the size lock measures from the point inside this box that is closest to the object. In Play mode (or if empty), the live camera position is used instead.")]
    public BoxCollider cameraBounds;

    public const float NearEpsilon = 0.01f;

    public Camera ResolveCamera()
    {
        return outputCamera != null ? outputCamera : Camera.main;
    }

    /// <summary>
    /// World-space AABB of the camera BoxCollider, built from its box corners so it also works
    /// while the collider is disabled (Collider.bounds is empty then).
    /// </summary>
    public bool TryGetCameraBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        if (cameraBounds == null)
        {
            return false;
        }

        Transform t = cameraBounds.transform;
        Vector3 c = cameraBounds.center;
        Vector3 h = cameraBounds.size * 0.5f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = c + new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
            Vector3 world = t.TransformPoint(corner);
            if (i == 0)
            {
                bounds = new Bounds(world, Vector3.zero);
            }
            else
            {
                bounds.Encapsulate(world);
            }
        }
        return true;
    }

    /// <summary>
    /// The camera position the size lock measures from: in Edit mode, the point of the camera bounds
    /// closest to worldPoint (if bounds are set). In Play mode (or if no bounds are set), the live camera position.
    /// </summary>
    public Vector3 GetReferenceCameraPosition(Vector3 worldPoint)
    {
        if (!Application.isPlaying && TryGetCameraBounds(out Bounds b))
        {
            return b.ClosestPoint(worldPoint);
        }
        Camera cam = ResolveCamera();
        return cam != null ? cam.transform.position : Vector3.zero;
    }

    public bool Validate(out string message)
    {
        Camera cam = ResolveCamera();
        if (cam == null)
        {
            message = "No output camera assigned and no Camera.main found.";
            return false;
        }
        if (cam.orthographic)
        {
            message = "Output camera is orthographic. This tool assumes a perspective camera.";
            return false;
        }
        if (referenceResolution.x <= 0 || referenceResolution.y <= 0)
        {
            message = "Reference resolution must be positive.";
            return false;
        }
        message = null;
        return true;
    }

    /// <summary>View matrix in Unity's camera convention (same as Camera.worldToCameraMatrix).</summary>
    public static Matrix4x4 GetViewMatrix(Vector3 cameraPosition, Quaternion cameraRotation)
    {
        Matrix4x4 cameraToWorld = Matrix4x4.TRS(cameraPosition, cameraRotation, Vector3.one);
        return Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * cameraToWorld.inverse;
    }

    /// <summary>
    /// Projects world points to pixels at the reference resolution and returns the size of
    /// their screen-space bounding box. Builds its own projection from fieldOfView + the
    /// reference aspect, so the Game view window size is irrelevant.
    /// </summary>
    public bool TryProject(Matrix4x4 view, float fieldOfView, Vector3[] worldPoints, out Vector2 pixelSize, out string error)
    {
        pixelSize = Vector2.zero;
        float aspect = (float)referenceResolution.x / referenceResolution.y;
        Matrix4x4 viewProjection = Matrix4x4.Perspective(fieldOfView, aspect, 0.1f, 100000f) * view;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < worldPoints.Length; i++)
        {
            Vector3 p = worldPoints[i];
            Vector4 clip = viewProjection * new Vector4(p.x, p.y, p.z, 1f);
            if (clip.w <= NearEpsilon)
            {
                error = "A corner of the object is at or behind the camera plane; projection is invalid.";
                return false;
            }
            float x = (clip.x / clip.w * 0.5f + 0.5f) * referenceResolution.x;
            float y = (clip.y / clip.w * 0.5f + 0.5f) * referenceResolution.y;
            min = Vector2.Min(min, new Vector2(x, y));
            max = Vector2.Max(max, new Vector2(x, y));
        }

        pixelSize = max - min;
        error = null;
        return true;
    }

    /// <summary>
    /// Independent closed-form check: pixels per world unit on a plane parallel to the
    /// camera at the given depth = referenceHeight / (2 * depth * tan(fov / 2)).
    /// </summary>
    public float GetPixelsPerWorldUnit(float depth, float fieldOfView)
    {
        return referenceResolution.y / (2f * depth * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad));
    }
}