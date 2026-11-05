using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Setup E Icon, Dad Voice & Night Light
// - "E" icon (prefab: quad + Alagard 'E') over interactables, in front of doors
// - Dad (Mr Lockbell) talks with a deep, pitched-down voice blip
// - Nora's light goes 0.7 -> 0.22 when it becomes night
// Safe to run again.
public static class PolishSetup
{
    private const string OldIconPath = "Assets/Art/UI/InteractIcon_E.png";
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string PrefabPath = PrefabFolder + "/InteractIcon.prefab";
    private const string MaterialPath = PrefabFolder + "/InteractIcon_Key.mat";
    private const string BorderMaterialPath = PrefabFolder + "/InteractIcon_Border.mat";
    private const string FontPath = "Assets/Fonts/alagard SDF.asset";
    private const string DadSpeaker = "Mr Lockbell";

    [MenuItem("Tools/Sleepover/Setup E Icon, Dad Voice & Night Light")]
    public static void Setup()
    {
        var report = new List<string>();
        PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        CharacterMovement player = Object.FindFirstObjectByType<CharacterMovement>(FindObjectsInactive.Include);
        if (interactor == null || player == null)
        {
            EditorUtility.DisplayDialog("Setup", "No player in the open scene.", "OK");
            return;
        }

        // ---------- E icon (prefab: quad + TMP 'E') ----------
        InteractionIcon icon = PlaceIconPrefab(report);

        var interSO = new SerializedObject(interactor);
        interSO.FindProperty("worldIcon").objectReferenceValue = icon;
        interSO.FindProperty("showTextPrompt").boolValue = false;
        interSO.ApplyModifiedProperties();
        report.Add("Prompt: 'E' icon above objects, doors and people (no more text at the bottom).");

        // ---------- Dad's voice ----------
        DialogueUI ui = null;
        foreach (DialogueUI candidate in Object.FindObjectsByType<DialogueUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (new SerializedObject(candidate).FindProperty("speakerVoices").arraySize > 0) ui = candidate;
        if (ui != null)
        {
            var uiSO = new SerializedObject(ui);
            SerializedProperty voices = uiSO.FindProperty("speakerVoices");
            SerializedProperty dad = null;
            Object blip = null;
            for (int i = 0; i < voices.arraySize; i++)
            {
                SerializedProperty v = voices.GetArrayElementAtIndex(i);
                string name = v.FindPropertyRelative("speakerName").stringValue;
                if (name.Trim().ToLower() == DadSpeaker.ToLower()) dad = v;
                if (blip == null) blip = v.FindPropertyRelative("blip").objectReferenceValue;   // same sound as the others
            }
            if (dad == null)
            {
                voices.arraySize++;
                dad = voices.GetArrayElementAtIndex(voices.arraySize - 1);
                dad.FindPropertyRelative("speakerName").stringValue = DadSpeaker;
                dad.FindPropertyRelative("blip").objectReferenceValue = blip;
                dad.FindPropertyRelative("minPitch").floatValue = 0.5f;
                dad.FindPropertyRelative("maxPitch").floatValue = 0.65f;
                uiSO.ApplyModifiedProperties();
                report.Add("Dad's voice: same blip, pitched down (0.5 - 0.65).");
            }
            else
            {
                report.Add("Dad's voice already set (kept).");
            }
        }

        // ---------- Nora's light at night ----------
        MovieSequence movie = Object.FindFirstObjectByType<MovieSequence>(FindObjectsInactive.Include);
        Light noraLight = player.GetComponentInChildren<Light>(true);
        if (movie != null && noraLight != null)
        {
            var movieSO = new SerializedObject(movie);
            SerializedProperty changes = movieSO.FindProperty("nightLightChanges");
            bool listed = false;
            for (int i = 0; i < changes.arraySize; i++)
                if (changes.GetArrayElementAtIndex(i).FindPropertyRelative("light").objectReferenceValue == noraLight) listed = true;
            if (!listed)
            {
                changes.arraySize++;
                SerializedProperty c = changes.GetArrayElementAtIndex(changes.arraySize - 1);
                c.FindPropertyRelative("light").objectReferenceValue = noraLight;
                c.FindPropertyRelative("nightIntensity").floatValue = 0.22f;
                movieSO.ApplyModifiedProperties();
            }
            report.Add("Nora's light: " + noraLight.intensity + " -> 0.22 at night.");
        }

        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        report.Add("Scene saved.");
        EditorUtility.DisplayDialog("Setup", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }
    // Builds Assets/Prefabs/UI/InteractIcon.prefab once (edit it by hand afterwards),
    // and puts one in the scene instead of the old sprite icon.
    private static InteractionIcon PlaceIconPrefab(List<string> report)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            prefab = BuildPrefab();
            report.Add("Created the prefab " + PrefabPath + " (quad + Alagard 'E'): edit it as you like.");
        }

        // Replace the old icon object(s) in the scene.
        foreach (InteractionIcon old in Object.FindObjectsByType<InteractionIcon>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (PrefabUtility.GetCorrespondingObjectFromSource(old.gameObject) != prefab)
                Undo.DestroyObjectImmediate(old.gameObject);

        InteractionIcon icon = Object.FindFirstObjectByType<InteractionIcon>(FindObjectsInactive.Include);
        if (icon == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Place icon");
            icon = instance.GetComponent<InteractionIcon>();
        }

        // The old pixel texture isn't used anymore.
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(OldIconPath) != null)
        {
            AssetDatabase.DeleteAsset(OldIconPath);
            report.Add("Removed the old E texture.");
        }
        return icon;
    }

    // Tools > Sleepover > Update E Icon (Label)
    // Quad background (Border + Key) behind the Alagard 'E', plus the "Inspect / Enter..." text above it.
    // Keeps your edits to "Letter E". Safe to run again.
    [MenuItem("Tools/Sleepover/Update E Icon (Label)")]
    public static void UpdateIconLook()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            EditorUtility.DisplayDialog("E Icon", "No prefab at " + PrefabPath + ". Run 'Setup E Icon, Dad Voice & Night Light' first.", "OK");
            return;
        }

        TMPro.TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontPath);
        Material outlined = MakeOutlinedFontMaterial(font);
        Material keyMat = MakeMaterial(MaterialPath, new Color(0.92f, 0.92f, 0.95f, 1f));
        Material borderMat = MakeMaterial(BorderMaterialPath, new Color(0.08f, 0.08f, 0.12f, 1f));

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        foreach (string old in new[] { "Border", "Key", "Label" })
        {
            Transform t = root.transform.Find(old);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        MakeQuad("Border", root.transform, borderMat, KeySize, 0.002f, 100);
        MakeQuad("Key", root.transform, keyMat, 0.30f, 0f, 101);

        Transform letter = root.transform.Find("Letter E");
        if (letter != null)
        {
            letter.localPosition = new Vector3(0f, 0f, -0.002f);
            letter.GetComponent<MeshRenderer>().sortingOrder = 102;
        }

        // "Inspect", "Enter"... above the key, 1.5x the key's width.
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(root.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, KeySize * 0.5f + 0.09f, -0.002f);
        var label = labelGO.AddComponent<TMPro.TextMeshPro>();
        label.text = "Inspect";
        label.font = font;
        if (outlined != null) label.fontSharedMaterial = outlined;
        label.color = new Color(0.95f, 0.95f, 0.97f, 1f);
        label.alignment = TMPro.TextAlignmentOptions.Bottom;
        label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = 0.4f;
        label.fontSizeMax = 1.6f;
        label.rectTransform.sizeDelta = new Vector2(KeySize * 1.5f, 0.16f);
        label.GetComponent<MeshRenderer>().sortingOrder = 102;

        var iconSO = new SerializedObject(root.GetComponent<InteractionIcon>());
        iconSO.FindProperty("label").objectReferenceValue = label;
        iconSO.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        // The pixel keycap texture isn't used anymore.
        AssetDatabase.DeleteAsset(KeycapPath);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("E Icon", "Done!\n\n- Quad background behind the 'E'.\n- Text above it (Inspect, Enter...) in Alagard, " +
            "1.5x the key's width.\n\nEdit it: double-click " + PrefabPath, "OK");
    }

    private const float KeySize = 0.36f;
    private const string KeycapPath = "Assets/Art/UI/InteractIcon_Key.png";
    private const string OutlineMatPath = PrefabFolder + "/alagard SDF Outline.mat";

    private static GameObject MakeQuad(string name, Transform parent, Material mat, float size, float z, int order)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<Collider>());   // never blocks Nora or the interaction search
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = new Vector3(0f, 0f, z);
        quad.transform.localScale = new Vector3(size, size, 1f);
        var r = quad.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.sortingOrder = order;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return quad;
    }

    // Sprites/Default: unlit (readable in the dark) and supports fading.
    private static Material MakeMaterial(string path, Color color)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Sprites/Default")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // A copy of the Alagard material with a dark outline, so the label reads on any background.
    private static Material MakeOutlinedFontMaterial(TMPro.TMP_FontAsset font)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMatPath);
        if (mat != null || font == null) return mat;
        mat = new Material(font.material);
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat("_OutlineWidth", 0.25f);
        mat.SetColor("_OutlineColor", new Color(0.08f, 0.08f, 0.12f, 1f));
        AssetDatabase.CreateAsset(mat, OutlineMatPath);
        return mat;
    }

    private static GameObject BuildPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        var root = new GameObject("InteractIcon");
        root.AddComponent<InteractionIcon>();

        var letter = new GameObject("Letter E");
        letter.transform.SetParent(root.transform, false);
        letter.transform.localPosition = new Vector3(0f, 0f, -0.002f);
        var tmp = letter.AddComponent<TMPro.TextMeshPro>();
        tmp.text = "E";
        tmp.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontPath);
        tmp.fontSize = 2.4f;
        tmp.color = new Color(0.1f, 0.1f, 0.16f, 1f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(0.3f, 0.3f);
        tmp.GetComponent<MeshRenderer>().sortingOrder = 102;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        UpdateIconLook();   // quads + label
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }
}