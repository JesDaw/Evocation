using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for EnvironmentArtAsset: lock status, sorting readout, artist pixel/PPU specs,
/// and the calculation-vs-camera verification readout.
/// </summary>
[CustomEditor(typeof(EnvironmentArtAsset))]
public class EnvironmentArtAssetEditor : Editor
{
    readonly Vector3[] corners = new Vector3[4];

    public override bool RequiresConstantRepaint()
    {
        return true;
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
        DrawArtistSpecs(asset);
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

    // EnvironmentArtAssetEditor.DrawArtistSpecs
    void DrawArtistSpecs(EnvironmentArtAsset asset)
    {
        EditorGUILayout.LabelField("Artist Specifications & PPU", EditorStyles.boldLabel);
        EnvironmentAssetSpec spec = asset.CalculateSpec();
        if (!spec.isValid)
        {
            EditorGUILayout.HelpBox(spec.error, MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Intended World Size", $"{spec.worldSize.x:F2} x {spec.worldSize.y:F2} units");
        EditorGUILayout.LabelField("Max Projected Size (Closest)", FormatPixels(spec.maxProjectedPixels));
        EditorGUILayout.LabelField("Min Projected Size (Farthest)", FormatPixels(spec.minProjectedPixels));
        EditorGUILayout.LabelField("Recommended Canvas Size", $"{spec.recommendedSourcePixels.x} x {spec.recommendedSourcePixels.y} px");
        EditorGUILayout.LabelField("Recommended Final PPU (at scale 1)", spec.recommendedPPU.ToString("F2"));

        if (spec.hasCurrentSprite)
        {
            EditorGUILayout.LabelField("Current Sprite Pixels / PPU", $"{spec.currentSpritePixels.x} x {spec.currentSpritePixels.y} px  @  {spec.currentSpritePPU:F1} PPU");
            float importPPUForCurrentTexture = spec.worldSize.x > 0.0001f ? spec.currentSpritePixels.x / spec.worldSize.x : 100f;
            EditorGUILayout.LabelField("PPU to Reset Scale Now", importPPUForCurrentTexture.ToString("F2"));
        }

        if (spec.isUndersized || spec.exceedsMaxTextureSize)
        {
            EditorGUILayout.HelpBox(spec.warnings, MessageType.Warning);
        }
    }

    // EnvironmentArtAssetEditor.DrawVerification
    void DrawVerification(EnvironmentArtAsset asset, EnvironmentArtSettings settings, Camera cam, Vector3 referencePoint, float depth)
    {
        EditorGUILayout.LabelField("Verification (pixels at reference resolution)", EditorStyles.boldLabel);

        asset.GetWorldCorners(corners);

        Vector2 formula = asset.GetWorldSize() * settings.GetPixelsPerWorldUnit(depth, cam.fieldOfView);
        EditorGUILayout.LabelField("A. Formula at reference point", FormatPixels(formula));

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