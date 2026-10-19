using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Sleepover > Style Choice Buttons
// Restyles the ChoiceButton prefab (dark teal box, light border, pixel font,
// hover effect) and stops the choice list from stretching buttons to full width.
// Run it again any time; tweak the values below to change the look.
public static class ChoiceButtonStyle
{
    private const string ChoiceButtonGuidSearch = "ChoiceButton t:Prefab";
    private const string MinecraftFontGuid = "9e804c38164613644999593dbe0d40d6";

    private static readonly Color BoxColor = new Color(0.08f, 0.27f, 0.30f, 0.95f);
    private static readonly Color BorderColor = new Color(0.55f, 1f, 1f, 0.9f);
    private static readonly Color TextColor = Color.white;

    private const float ButtonWidth = 560f;
    private const float ButtonHeight = 64f;
    private const float FontSize = 30f;

    [MenuItem("Tools/Sleepover/Style Choice Buttons")]
    public static void Style()
    {
        string prefabPath = null;
        foreach (string guid in AssetDatabase.FindAssets(ChoiceButtonGuidSearch))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == "ChoiceButton")
                prefabPath = path;
        }

        if (prefabPath == null)
        {
            EditorUtility.DisplayDialog("Style Choice Buttons", "ChoiceButton prefab not found.", "OK");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(MinecraftFontGuid));

        // ---------- Prefab ----------
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        Image box = root.GetComponent<Image>();
        if (box != null)
        {
            box.sprite = null;          // flat pixel-style box instead of Unity's rounded button
            box.color = BoxColor;
            box.type = Image.Type.Simple;
        }

        Outline outline = root.GetComponent<Outline>();
        if (outline == null) outline = root.AddComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = root.GetComponent<Button>();
        if (button != null)
        {
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = box;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.colorMultiplier = 1.6f;   // hover brightens the teal box
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        LayoutElement layout = root.GetComponent<LayoutElement>();
        if (layout == null) layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = ButtonWidth;
        layout.minHeight = ButtonHeight;
        layout.preferredHeight = ButtonHeight;

        if (root.GetComponent<MenuButtonHover>() == null)
            root.AddComponent<MenuButtonHover>();

        TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            if (font != null) text.font = font;
            text.fontSize = FontSize;
            text.enableAutoSizing = false;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 6f);
            textRect.offsetMax = new Vector2(-18f, -6f);
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        // ---------- Choice lists in the open scene: center, don't stretch ----------
        int panels = 0;
        DialogueUI[] uis = Object.FindObjectsByType<DialogueUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (DialogueUI ui in uis)
        {
            var so = new SerializedObject(ui);
            GameObject choicesPanel = so.FindProperty("choicesPanel").objectReferenceValue as GameObject;
            if (choicesPanel == null)
                continue;

            VerticalLayoutGroup list = choicesPanel.GetComponent<VerticalLayoutGroup>();
            if (list != null)
            {
                Undo.RecordObject(list, "Style choices");
                list.childAlignment = TextAnchor.MiddleCenter;
                list.childControlWidth = true;
                list.childControlHeight = true;
                list.childForceExpandWidth = false;
                list.childForceExpandHeight = false;
                list.spacing = 14f;
                panels++;
            }

            // The list itself shouldn't draw a big white box behind the buttons.
            Image listBackground = choicesPanel.GetComponent<Image>();
            if (listBackground != null)
            {
                Undo.RecordObject(listBackground, "Style choices");
                listBackground.color = new Color(0f, 0f, 0f, 0f);
            }
        }

        if (uis.Length > 0)
        {
            EditorSceneManager.MarkSceneDirty(uis[0].gameObject.scene);
            EditorSceneManager.SaveScene(uis[0].gameObject.scene);
        }

        EditorUtility.DisplayDialog("Style Choice Buttons",
            "Done!\n\n- ChoiceButton restyled (font: " + (font != null ? font.name : "unchanged") + ")\n- " +
            panels + " choice list(s) centered, no more stretching\n- Scene saved.", "OK");
    }
}
