using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for EnvironmentArtAsset: lock status, sorting readout, and the
/// calculation-vs-camera verification readout.
/// </summary>
[CustomEditor(typeof(EnvironmentArtAsset))]
public class EnvironmentArtAssetEditor : Editor
{
    readonly Vector3[] corners = new Vector3[4];

    public override bool RequiresConstantRepaint()
    {
        return true; // keeps the numbers live while dragging in the Scene view
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnvironmentArtAsset asset = (EnvironmentArtAsset)target;
        EnvironmentArtSettings settings = asset.settings;

        EditorGUILayout.Space();
        if (settings == null)
        {
            EditorGUILayout.HelpBox("No EnvironmentArtSettings found. Assign one or add it to the scene.", MessageType.Error);
            return;
        }
        if (!settings.Validate(out string problem))
        {
            EditorGUILayout.HelpBox(problem, MessageType.Error);
            return;
        }

        Camera cam = settings.ResolveCamera();
        Quaternion rotation = cam.transform.rotation;
        Vector3 forward = rotation * Vector3.forward;

        // Reference point: the locked anchor's if there is one, otherwise the active reference point right now.
        Vector3 referencePoint = asset.lockApparentSize && asset.hasAnchor
            ? asset.anchorCameraPosition
            : settings.GetReferenceCameraPosition(asset.transform.position);
        float depth = Vector3.Dot(asset.transform.position - referencePoint, forward);

        DrawSorting(asset);
        EditorGUILayout.Space();

        bool usingBounds = !Application.isPlaying && settings.cameraBounds != null;
        EditorGUILayout.LabelField("Reference camera point", referencePoint.ToString("F2") + (usingBounds ? "  (closest in camera bounds)" : "  (live camera)"));
        EditorGUILayout.LabelField("Depth from reference point", depth.ToString("F3"));
        if (depth <= EnvironmentArtSettings.NearEpsilon)
        {
            EditorGUILayout.HelpBox("Object is at or behind the reference camera plane (inside or past the camera bounds). Projection and lock are invalid here; the lock leaves it alone.", MessageType.Error);
            return;
        }

        DrawLock(asset, settings);
        EditorGUILayout.Space();
        DrawVerification(asset, settings, cam, referencePoint, depth);
    }

    // EnvironmentArtAssetEditor.DrawSorting
    void DrawSorting(EnvironmentArtAsset asset)
    {
        Renderer r = asset.GetComponent<Renderer>();
        if (r == null)
        {
            return;
        }
        EditorGUILayout.LabelField("Sorting order (current / expected)", r.sortingOrder + " / " + asset.GetExpectedSortingOrder());
    }

    // EnvironmentArtAssetEditor.DrawLock
    void DrawLock(EnvironmentArtAsset asset, EnvironmentArtSettings settings)
    {
        EditorGUILayout.LabelField("Apparent-size lock", EditorStyles.boldLabel);

        if (!asset.lockApparentSize)
        {
            EditorGUILayout.HelpBox("Lock is off: scale and position are yours to edit.", MessageType.None);
            return;
        }
        if (!asset.hasAnchor)
        {
            EditorGUILayout.HelpBox("Lock is on but not anchored yet. It anchors on the next update.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Anchor depth", asset.anchorDepth.ToString("F3"));
        EditorGUILayout.LabelField("Anchor scale", asset.anchorScale.ToString("F4"));
        EditorGUILayout.LabelField("Anchor projected size", FormatPixels(asset.anchorProjectedPixels));

        // Direct check of the lock: project the object as it is now from the anchor's reference point.
        asset.GetWorldCorners(corners);
        Matrix4x4 anchorView = EnvironmentArtSettings.GetViewMatrix(asset.anchorCameraPosition, asset.anchorCameraRotation);
        if (settings.TryProject(anchorView, asset.anchorFieldOfView, corners, out Vector2 nowPixels, out _))
        {
            Vector2 d = nowPixels - asset.anchorProjectedPixels;
            EditorGUILayout.LabelField("Now vs anchor size", "dW " + d.x.ToString("F2") + "   dH " + d.y.ToString("F2") + " px");
        }

        string modeNote = Application.isPlaying
            ? "Play mode: reference point is the live camera. Moving the camera, moving X/Y, or scaling by hand re-anchors."
            : "Edit mode: reference point is the closest point in camera bounds (live camera position is ignored). Moving X/Y, scaling by hand, or changing bounds re-anchors.";
        EditorGUILayout.HelpBox("Change only Z and the object keeps its size and screen position as seen from the reference point. " + modeNote, MessageType.None);
    }

    // EnvironmentArtAssetEditor.DrawVerification
    void DrawVerification(EnvironmentArtAsset asset, EnvironmentArtSettings settings, Camera cam, Vector3 referencePoint, float depth)
    {
        EditorGUILayout.LabelField("Verification (pixels at reference resolution)", EditorStyles.boldLabel);

        asset.GetWorldCorners(corners);

        // A: closed-form formula at the reference point.
        Vector2 formula = asset.GetWorldSize() * settings.GetPixelsPerWorldUnit(depth, cam.fieldOfView);
        EditorGUILayout.LabelField("A. Formula at reference point", FormatPixels(formula));

        // B: our own matrix projection from the reference point.
        Matrix4x4 ownView = EnvironmentArtSettings.GetViewMatrix(referencePoint, cam.transform.rotation);
        bool haveB = settings.TryProject(ownView, cam.fieldOfView, corners, out Vector2 matrixOwn, out string errB);
        if (haveB)
        {
            EditorGUILayout.LabelField("B. Own projection at reference point", FormatPixels(matrixOwn));
            EditorGUILayout.LabelField("A vs B", "dW " + (formula.x - matrixOwn.x).ToString("F2") + "   dH " + (formula.y - matrixOwn.y).ToString("F2") + " px");
        }
        else
        {
            EditorGUILayout.HelpBox("B: " + errB, MessageType.Warning);
        }

        // C: Unity's own camera matrix, from wherever the live camera currently is.
        bool haveC = settings.TryProject(cam.worldToCameraMatrix, cam.fieldOfView, corners, out Vector2 matrixUnity, out string errC);
        if (haveC)
        {
            EditorGUILayout.LabelField("C. Live camera (Unity matrix)", FormatPixels(matrixUnity));
            if (haveB && Vector3.Distance(cam.transform.position, referencePoint) < 0.001f)
            {
                EditorGUILayout.LabelField("B vs C", "dW " + (matrixOwn.x - matrixUnity.x).ToString("F2") + "   dH " + (matrixOwn.y - matrixUnity.y).ToString("F2") + " px");
            }
            else
            {
                EditorGUILayout.HelpBox("The live camera is not at the reference point, so C differs from A/B by design. Put the camera at the reference point to compare B with C.", MessageType.None);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("C: " + errC, MessageType.Warning);
        }

        if (Quaternion.Angle(asset.transform.rotation, cam.transform.rotation) > 0.5f)
        {
            EditorGUILayout.HelpBox("Object is rotated relative to the camera. Formula A assumes a plane facing the camera; B and C are still valid.", MessageType.None);
        }
    }

    // EnvironmentArtAssetEditor.FormatPixels
    static string FormatPixels(Vector2 p)
    {
        return p.x.ToString("F1") + " x " + p.y.ToString("F1") + " px";
    }
}

/// <summary>Inspector for EnvironmentArtSettings: validation and the camera-bounds readout.</summary>
[CustomEditor(typeof(EnvironmentArtSettings))]
public class EnvironmentArtSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EnvironmentArtSettings settings = (EnvironmentArtSettings)target;
        if (!settings.Validate(out string problem))
        {
            EditorGUILayout.HelpBox(problem, MessageType.Error);
        }

        if (settings.TryGetCameraBounds(out Bounds b))
        {
            EditorGUILayout.LabelField("Camera bounds min", b.min.ToString("F2"));
            EditorGUILayout.LabelField("Camera bounds max", b.max.ToString("F2"));
        }
        else
        {
            EditorGUILayout.HelpBox("No camera bounds assigned: the size lock falls back to the live camera position. Assign your camera confiner BoxCollider.", MessageType.Warning);
        }
    }
}