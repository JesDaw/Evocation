using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// EditorWindow for reviewing all EnvironmentArtAssets in the scene and exporting
/// Per-Asset Art Templates & Specifications (CSV + Procreate Artist Markdown Guide).
/// </summary>
public class EnvironmentArtEditor : EditorWindow
{
    Vector2 scrollPos;

    [MenuItem("Tools/Environment Art/Blocking & Spec Exporter")]
    public static void OpenWindow()
    {
        EnvironmentArtEditor window = GetWindow<EnvironmentArtEditor>("Env Art Specs");
        window.minSize = new Vector2(720, 420);
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    void OnGUI()
    {
        EnvironmentArtSettings settings = FindFirstObjectByType<EnvironmentArtSettings>();
        if (settings == null)
        {
            EditorGUILayout.HelpBox("No EnvironmentArtSettings found in the active scene. Add an EnvironmentArtSettings component first.", MessageType.Error);
            return;
        }

        DrawHeader(settings);
        EditorGUILayout.Space();

        if (!settings.Validate(out string problem))
        {
            EditorGUILayout.HelpBox(problem, MessageType.Error);
            return;
        }

        EnvironmentArtAsset[] assets = FindObjectsByType<EnvironmentArtAsset>(FindObjectsSortMode.None);
        // Sort back-to-front (largest Z first) to match the artist's painting order.
        Array.Sort(assets, (a, b) => b.transform.position.z.CompareTo(a.transform.position.z));

        DrawExportButtons(settings, assets);
        EditorGUILayout.Space();
        DrawAssetTable(assets);
    }

    // EnvironmentArtEditor.DrawHeader
    void DrawHeader(EnvironmentArtSettings settings)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Environment Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        Camera cam = (Camera)EditorGUILayout.ObjectField("Output Camera", settings.outputCamera, typeof(Camera), true);
        BoxCollider bounds = (BoxCollider)EditorGUILayout.ObjectField("Camera Bounds", settings.cameraBounds, typeof(BoxCollider), true);
        Vector2Int res = EditorGUILayout.Vector2IntField("Reference Resolution", settings.referenceResolution);
        float density = EditorGUILayout.FloatField("Pixel Density Multiplier", settings.pixelDensityMultiplier);
        int maxTex = EditorGUILayout.IntField("Max Texture Warning (px)", settings.maxTextureSizeWarning);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Resolution Presets");
        if (GUILayout.Button("1080p (1920x1080)"))
        {
            res = new Vector2Int(1920, 1080);
        }
        if (GUILayout.Button("4K (3840x2160)"))
        {
            res = new Vector2Int(3840, 2160);
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(settings, "Modify Environment Art Settings");
            settings.outputCamera = cam;
            settings.cameraBounds = bounds;
            settings.referenceResolution = new Vector2Int(Mathf.Max(1, res.x), Mathf.Max(1, res.y));
            settings.pixelDensityMultiplier = Mathf.Max(0.1f, density);
            settings.maxTextureSizeWarning = Mathf.Max(256, maxTex);
            EditorUtility.SetDirty(settings);
        }
        EditorGUILayout.EndVertical();
    }

    // EnvironmentArtEditor.DrawExportButtons
    void DrawExportButtons(EnvironmentArtSettings settings, EnvironmentArtAsset[] assets)
    {
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = assets.Length > 0;
        if (GUILayout.Button("Export CSV Specifications...", GUILayout.Height(28)))
        {
            ExportCSV(settings, assets);
        }
        if (GUILayout.Button("Export Procreate Artist Guide (.md)...", GUILayout.Height(28)))
        {
            ExportMarkdownGuide(settings, assets);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    // EnvironmentArtEditor.DrawAssetTable
    void DrawAssetTable(EnvironmentArtAsset[] assets)
    {
        EditorGUILayout.LabelField($"Scene Assets ({assets.Length}) — Sorted Back-to-Front (Z)", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Header Row
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Asset", EditorStyles.miniBoldLabel, GUILayout.Width(140));
        GUILayout.Label("Role", EditorStyles.miniBoldLabel, GUILayout.Width(110));
        GUILayout.Label("Z Depth", EditorStyles.miniBoldLabel, GUILayout.Width(65));
        GUILayout.Label("Lock", EditorStyles.miniBoldLabel, GUILayout.Width(40));
        GUILayout.Label("World Size", EditorStyles.miniBoldLabel, GUILayout.Width(95));
        GUILayout.Label("Target Canvas (px)", EditorStyles.miniBoldLabel, GUILayout.Width(125));
        GUILayout.Label("Rec. PPU", EditorStyles.miniBoldLabel, GUILayout.Width(65));
        GUILayout.Label("Status / Warnings", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        foreach (EnvironmentArtAsset asset in assets)
        {
            EnvironmentAssetSpec spec = asset.CalculateSpec();

            EditorGUILayout.BeginHorizontal("box");
            if (GUILayout.Button(asset.name, EditorStyles.miniButton, GUILayout.Width(140)))
            {
                Selection.activeGameObject = asset.gameObject;
                EditorGUIUtility.PingObject(asset.gameObject);
            }

            EditorGUI.BeginChangeCheck();
            EnvironmentAssetRole newRole = (EnvironmentAssetRole)EditorGUILayout.EnumPopup(asset.role, GUILayout.Width(110));
            float newZ = EditorGUILayout.FloatField(asset.transform.position.z, GUILayout.Width(65));
            bool newLock = EditorGUILayout.Toggle(asset.lockApparentSize, GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "Update Environment Asset");
                Undo.RecordObject(asset.transform, "Update Environment Asset Z");
                asset.role = newRole;
                asset.lockApparentSize = newLock;
                Vector3 p = asset.transform.position;
                p.z = newZ;
                asset.transform.position = p;
                EditorUtility.SetDirty(asset);
            }

            if (spec.isValid)
            {
                GUILayout.Label($"{spec.worldSize.x:F1} x {spec.worldSize.y:F1}", GUILayout.Width(95));
                GUILayout.Label($"{spec.recommendedSourcePixels.x} x {spec.recommendedSourcePixels.y}", GUILayout.Width(125));
                GUILayout.Label($"{spec.recommendedPPU:F1}", GUILayout.Width(65));
                GUILayout.Label(spec.warnings, GUILayout.ExpandWidth(true));
            }
            else
            {
                GUILayout.Label(spec.error, GUILayout.ExpandWidth(true));
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    // EnvironmentArtEditor.ExportCSV
    void ExportCSV(EnvironmentArtSettings settings, EnvironmentArtAsset[] assets)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = "Environment";
        }

        string path = EditorUtility.SaveFilePanel(
            "Export Environment Art Specifications CSV",
            Application.dataPath,
            $"{sceneName}_ArtSpecs.csv",
            "csv"
        );

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Asset Name,Role,Z Position,Sorting Order,World Width,World Height,Closest Cam Distance,Max Projected Width (px),Max Projected Height (px),Min Projected Width (px),Min Projected Height (px),Target Canvas Width (px),Target Canvas Height (px),Recommended Final PPU (Scale 1),Current Sprite Width (px),Current Sprite Height (px),Current Sprite PPU,Warnings,Artist Notes");

        foreach (EnvironmentArtAsset asset in assets)
        {
            EnvironmentAssetSpec spec = asset.CalculateSpec();
            if (!spec.isValid)
            {
                sb.AppendLine($"{EscapeCSV(asset.name)},{asset.role},{asset.transform.position.z:F2},{asset.GetExpectedSortingOrder()},INVALID,,,,,,,,,,,," +
                              $"{EscapeCSV(spec.error)},{EscapeCSV(asset.artistNotes)}");
                continue;
            }

            string curW = spec.hasCurrentSprite ? spec.currentSpritePixels.x.ToString() : "N/A";
            string curH = spec.hasCurrentSprite ? spec.currentSpritePixels.y.ToString() : "N/A";
            string curPPU = spec.hasCurrentSprite ? spec.currentSpritePPU.ToString("F2") : "N/A";

            sb.AppendLine(string.Join(",",
                EscapeCSV(asset.name),
                asset.role.ToString(),
                asset.transform.position.z.ToString("F2"),
                asset.GetExpectedSortingOrder().ToString(),
                spec.worldSize.x.ToString("F3"),
                spec.worldSize.y.ToString("F3"),
                spec.depthAtClosestCamera.ToString("F3"),
                spec.maxProjectedPixels.x.ToString("F1"),
                spec.maxProjectedPixels.y.ToString("F1"),
                spec.minProjectedPixels.x.ToString("F1"),
                spec.minProjectedPixels.y.ToString("F1"),
                spec.recommendedSourcePixels.x.ToString(),
                spec.recommendedSourcePixels.y.ToString(),
                spec.recommendedPPU.ToString("F2"),
                curW,
                curH,
                curPPU,
                EscapeCSV(spec.warnings),
                EscapeCSV(asset.artistNotes)
            ));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        EditorUtility.RevealInFinder(path);
        Debug.Log($"Exported Environment Art CSV Specifications to: {path}");
    }

    // EnvironmentArtEditor.ExportMarkdownGuide
    void ExportMarkdownGuide(EnvironmentArtSettings settings, EnvironmentArtAsset[] assets)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = "Environment";
        }

        string path = EditorUtility.SaveFilePanel(
            "Export Procreate Artist Guide (.md)",
            Application.dataPath,
            $"{sceneName}_ArtistGuide.md",
            "md"
        );

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"# {sceneName} — Procreate Art & Layer Specification Guide");
        sb.AppendLine();
        sb.AppendLine($"* **Target Game Resolution:** {settings.referenceResolution.x} x {settings.referenceResolution.y} px");
        sb.AppendLine($"* **Quality / Density Factor:** {settings.pixelDensityMultiplier:F2}x");
        sb.AppendLine($"* **Painting Order:** Listed below from **Back to Front** (farthest background to closest foreground).");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Quick Summary Table (Back-to-Front)");
        sb.AppendLine();
        sb.AppendLine("| Order | Asset / Layer Name | Type | Depth (Z) | Procreate Canvas Size (W x H px) | Notes |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");

        for (int i = 0; i < assets.Length; i++)
        {
            EnvironmentArtAsset asset = assets[i];
            EnvironmentAssetSpec spec = asset.CalculateSpec();
            string sizeStr = spec.isValid
                ? $"**{spec.recommendedSourcePixels.x} x {spec.recommendedSourcePixels.y} px**"
                : $"*Invalid ({spec.error})*";
            string notes = string.IsNullOrWhiteSpace(asset.artistNotes) ? "-" : asset.artistNotes;
            sb.AppendLine($"| {i + 1} | **{asset.name}** | {asset.role} | {asset.transform.position.z:F1} | {sizeStr} | {notes} |");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Per-Asset Templates & Details");
        sb.AppendLine();

        for (int i = 0; i < assets.Length; i++)
        {
            EnvironmentArtAsset asset = assets[i];
            EnvironmentAssetSpec spec = asset.CalculateSpec();

            sb.AppendLine($"### {i + 1}. {asset.name}");
            sb.AppendLine($"* **Role:** {asset.role}");
            sb.AppendLine($"* **Scene Depth (Z):** `{asset.transform.position.z:F2}` (Sorting Order: `{asset.GetExpectedSortingOrder()}`)");

            if (!spec.isValid)
            {
                sb.AppendLine($"* **Status:** ⚠️ {spec.error}");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine($"* **Recommended Procreate Canvas Size:** **{spec.recommendedSourcePixels.x} x {spec.recommendedSourcePixels.y} px**");
            sb.AppendLine($"* **On-Screen Size Range:** {spec.minProjectedPixels.x:F0}x{spec.minProjectedPixels.y:F0} px (farthest zoom) up to {spec.maxProjectedPixels.x:F0}x{spec.maxProjectedPixels.y:F0} px (closest zoom)");
            sb.AppendLine($"* **Unity Import Settings (for Developer):** Set Transform Scale to `(1, 1, 1)` and Sprite PPU to `{spec.recommendedPPU:F2}` (World size: `{spec.worldSize.x:F2} x {spec.worldSize.y:F2}` units)");

            if (!string.IsNullOrWhiteSpace(asset.artistNotes))
            {
                sb.AppendLine($"* **Artist Notes:** {asset.artistNotes}");
            }
            if (spec.isUndersized || spec.exceedsMaxTextureSize)
            {
                sb.AppendLine($"* **Warnings:** ⚠️ {spec.warnings}");
            }
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        EditorUtility.RevealInFinder(path);
        Debug.Log($"Exported Procreate Artist Guide to: {path}");
    }

    static string EscapeCSV(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}