using TMPEffects.Components;
using TMPEffects.Databases.AnimationDatabase;
using TMPEffects.Databases.CommandDatabase;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Builds the throwaway TMPEffects validation scene so nobody has to hand-wire it.
public static class TMPEffectsValidationSceneBuilder
{
    const string ScenePath = "Assets/Code/Cutscenes/Dialogue/Prototype/TMPEffectsValidation.unity";
    const string ResourcesPackagePath = "Packages/com.luca3317.tmpeffects/Resources/TMPEffects Required Resources.unitypackage";
    const string SettingsResourceName = "TMPEffects Settings";

    [MenuItem("Tools/Dialogue Prototype/Build TMPEffects Validation Scene")]
    static void BuildScene()
    {
        // TMPEffectsSettings is internal to the package, so read its default databases through serialization.
        ScriptableObject settings = Resources.Load<ScriptableObject>(SettingsResourceName);
        if (settings == null)
        {
            AssetDatabase.ImportPackage(ResourcesPackagePath, false);
            EditorUtility.DisplayDialog("TMPEffects resources imported",
                "Imported the TMPEffects required resources into Assets/TMPEffects.\nRun this menu item again once the import finishes.", "OK");
            return;
        }

        var settingsObject = new SerializedObject(settings);
        Object animationDatabase = settingsObject.FindProperty("defaultAnimationDatabase").objectReferenceValue;
        Object commandDatabase = settingsObject.FindProperty("defaultCommandDatabase").objectReferenceValue;
        if (animationDatabase == null || commandDatabase == null)
        {
            Debug.LogError("[TMPEffectsValidationSceneBuilder] TMPEffects Settings has no default databases assigned.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Canvas canvas = CreateCanvas();

        Image background = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        background.transform.SetParent(canvas.transform, false);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one);
        background.color = new Color(0.06f, 0.06f, 0.09f, 1f);

        TMP_Text surfaceA = CreateSurface(canvas.transform, "TextSurface A", new Vector2(0.08f, 0.12f), new Vector2(0.62f, 0.42f),
            animationDatabase, commandDatabase);
        TMP_Text surfaceB = CreateSurface(canvas.transform, "TextSurface B", new Vector2(0.45f, 0.5f), new Vector2(0.92f, 0.78f),
            animationDatabase, commandDatabase);

        TMP_Text status = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        status.transform.SetParent(canvas.transform, false);
        Stretch(status.rectTransform, new Vector2(0.02f, 0.62f), new Vector2(0.42f, 0.98f));
        status.fontSize = 20;
        status.color = new Color(0.7f, 0.9f, 0.7f, 1f);
        status.alignment = TextAlignmentOptions.TopLeft;

        var validation = new GameObject("TMPEffectsValidation").AddComponent<TMPEffectsValidation>();
        var validationObject = new SerializedObject(validation);
        validationObject.FindProperty("surfaceA").objectReferenceValue = surfaceA;
        validationObject.FindProperty("surfaceB").objectReferenceValue = surfaceB;
        validationObject.FindProperty("statusText").objectReferenceValue = status;
        validationObject.FindProperty("animationDatabase").objectReferenceValue = animationDatabase;
        validationObject.FindProperty("commandDatabase").objectReferenceValue = commandDatabase;
        validationObject.ApplyModifiedPropertiesWithoutUndo();

        validationObject.Update();
        if (validationObject.FindProperty("animationDatabase").objectReferenceValue == null ||
            validationObject.FindProperty("commandDatabase").objectReferenceValue == null)
        {
            Debug.LogError("[TMPEffectsValidationSceneBuilder] Database references did not stick; scene not saved.");
            return;
        }

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        Debug.Log($"[TMPEffectsValidationSceneBuilder] Built {ScenePath}. Enter Play mode to run the checks.");
    }

    static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static TMP_Text CreateSurface(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Object animationDatabase, Object commandDatabase)
    {
        var surfaceObject = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        surfaceObject.transform.SetParent(parent, false);

        TMP_Text text = surfaceObject.GetComponent<TMP_Text>();
        Stretch(text.rectTransform, anchorMin, anchorMax);
        text.fontSize = 42;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.text = "";

        var writer = surfaceObject.AddComponent<TMPWriter>();
        var writerObject = new SerializedObject(writer);
        writerObject.FindProperty("useDefaultDatabase").boolValue = false;
        writerObject.FindProperty("writeOnStart").boolValue = false;
        writerObject.FindProperty("writeOnNewText").boolValue = true;
        writerObject.FindProperty("useScaledTime").boolValue = false;
        writerObject.ApplyModifiedPropertiesWithoutUndo();
        writer.SetDatabase((TMPCommandDatabase)commandDatabase);

        var animator = surfaceObject.AddComponent<TMPAnimator>();
        var animatorObject = new SerializedObject(animator);
        animatorObject.FindProperty("useDefaultDatabase").boolValue = false;
        animatorObject.FindProperty("context.useScaledTime").boolValue = false;
        animatorObject.ApplyModifiedPropertiesWithoutUndo();
        animator.SetDatabase((TMPAnimationDatabase)animationDatabase);
        EditorUtility.SetDirty(writer);
        EditorUtility.SetDirty(animator);

        return text;
    }

    static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
