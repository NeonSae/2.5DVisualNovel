using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Tools > Sleepover > Build Main Menu UI
// Builds the whole main menu (panels, buttons, settings, fade) in the open MainMenu scene.
public static class MainMenuBuilder
{
    private const string CanvasName = "MainMenuCanvas";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/SampleScene.unity";

    private static readonly Color TextColor = new Color(0.95f, 0.92f, 0.88f);
    private static readonly Color WindowColor = new Color(0.07f, 0.065f, 0.08f, 0.95f);
    private static readonly Color ButtonColor = new Color(0.12f, 0.11f, 0.13f, 0.85f);

    private static DefaultControls.Resources uiResources;
    private static TMP_DefaultControls.Resources tmpResources;

    [MenuItem("Tools/Sleepover/Build Main Menu UI")]
    public static void BuildMainMenu()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "MainMenu" &&
            !EditorUtility.DisplayDialog("Build Main Menu",
                "The open scene is '" + scene.name + "', not 'MainMenu'. Build the menu here anyway?",
                "Build here", "Cancel"))
        {
            return;
        }

        GameObject existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Build Main Menu",
                    "A '" + CanvasName + "' already exists. Delete it and build a new one?",
                    "Replace", "Cancel"))
            {
                return;
            }
            Undo.DestroyObjectImmediate(existing);
        }

        LoadResources();

        // ---------- Canvas ----------
        GameObject canvasGO = new GameObject(CanvasName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Main Menu");

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        AudioSource uiAudio = canvasGO.AddComponent<AudioSource>();
        uiAudio.playOnAwake = false;
        uiAudio.spatialBlend = 0f;

        // ---------- Main panel (left side) ----------
        GameObject mainPanel = CreateRect("MainPanel", canvasGO.transform);
        RectTransform mainRect = mainPanel.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0f, 0f);
        mainRect.anchorMax = new Vector2(0.45f, 1f);
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup mainLayout = mainPanel.AddComponent<VerticalLayoutGroup>();
        mainLayout.padding = new RectOffset(140, 40, 0, 0);
        mainLayout.spacing = 22;
        mainLayout.childAlignment = TextAnchor.MiddleLeft;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = true;
        mainLayout.childForceExpandWidth = false;
        mainLayout.childForceExpandHeight = false;

        TMP_Text title = CreateText("Title", mainPanel.transform, "SLEEPOVER", 110, TextAlignmentOptions.Left);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8;
        AddLayout(title.gameObject, 700, 150);

        CreateSpacer(mainPanel.transform, 30);

        Button playButton = CreateButton("PlayButton", mainPanel.transform, "Play", uiAudio);
        Button settingsButton = CreateButton("SettingsButton", mainPanel.transform, "Settings", uiAudio);
        Button quitButton = CreateButton("QuitButton", mainPanel.transform, "Quit", uiAudio);

        // ---------- Settings panel (dark overlay + window) ----------
        GameObject settingsPanel = CreateRect("SettingsPanel", canvasGO.transform);
        Stretch(settingsPanel.GetComponent<RectTransform>());
        Image dim = settingsPanel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject window = CreateRect("Window", settingsPanel.transform);
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(1000, 860);
        Image windowImage = window.AddComponent<Image>();
        windowImage.sprite = uiResources.standard;
        windowImage.type = Image.Type.Sliced;
        windowImage.color = WindowColor;

        VerticalLayoutGroup windowLayout = window.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(60, 60, 40, 40);
        windowLayout.spacing = 18;
        windowLayout.childAlignment = TextAnchor.UpperCenter;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        TMP_Text settingsTitle = CreateText("Title", window.transform, "SETTINGS", 64, TextAlignmentOptions.Center);
        settingsTitle.fontStyle = FontStyles.Bold;
        AddLayout(settingsTitle.gameObject, -1, 90);

        Slider masterSlider = CreateSliderRow(window.transform, "Master Volume");
        Slider musicSlider = CreateSliderRow(window.transform, "Music Volume");
        Slider sfxSlider = CreateSliderRow(window.transform, "Sound Effects");
        Slider brightnessSlider = CreateSliderRow(window.transform, "Brightness");
        Toggle fullscreenToggle = CreateToggleRow(window.transform, "Fullscreen");
        TMP_Dropdown resolutionDropdown = CreateDropdownRow(window.transform, "Resolution");
        TMP_Dropdown qualityDropdown = CreateDropdownRow(window.transform, "Quality");

        CreateSpacer(window.transform, 10);
        Button backButton = CreateButton("BackButton", window.transform, "Back", uiAudio);

        // ---------- Fade (on top of everything) ----------
        GameObject fade = CreateRect("Fade", canvasGO.transform);
        Stretch(fade.GetComponent<RectTransform>());
        Image fadeImage = fade.AddComponent<Image>();
        fadeImage.color = Color.black;
        fade.SetActive(false);

        // ---------- Components + references ----------
        ScreenFader fader = canvasGO.AddComponent<ScreenFader>();
        SetRef(fader, "fadeImage", fadeImage);

        SettingsMenu settings = settingsPanel.AddComponent<SettingsMenu>();
        SetRef(settings, "masterVolumeSlider", masterSlider);
        SetRef(settings, "musicVolumeSlider", musicSlider);
        SetRef(settings, "sfxVolumeSlider", sfxSlider);
        SetRef(settings, "brightnessSlider", brightnessSlider);
        SetRef(settings, "fullscreenToggle", fullscreenToggle);
        SetRef(settings, "resolutionDropdown", resolutionDropdown);
        SetRef(settings, "qualityDropdown", qualityDropdown);

        MainMenu menu = canvasGO.AddComponent<MainMenu>();
        SetRef(menu, "mainPanel", mainPanel);
        SetRef(menu, "settingsPanel", settingsPanel);
        SetRef(menu, "playButton", playButton);
        SetRef(menu, "settingsButton", settingsButton);
        SetRef(menu, "quitButton", quitButton);
        SetRef(menu, "backButton", backButton);
        SetRef(menu, "screenFader", fader);

        settingsPanel.SetActive(false);

        // ---------- Camera motion + brightness ----------
        string extras = "";

        CinemachineCamera cmCamera = Object.FindFirstObjectByType<CinemachineCamera>();
        if (cmCamera != null && cmCamera.GetComponent<MenuCameraMotion>() == null)
        {
            Undo.AddComponent<MenuCameraMotion>(cmCamera.gameObject);
            extras += "\n- Added Menu Camera Motion to '" + cmCamera.name + "'.";
        }

        extras += AddBrightnessToGlobalVolume();

        // ---------- Build list ----------
        extras += AddScenesToBuild();

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = canvasGO;

        EditorUtility.DisplayDialog("Build Main Menu",
            "Main menu built!" + extras + "\n\nDon't forget to save the scene (Ctrl+S).", "OK");
    }

    [MenuItem("Tools/Sleepover/Add Brightness To Global Volume")]
    public static void AddBrightnessMenuItem()
    {
        string result = AddBrightnessToGlobalVolume();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Brightness",
            string.IsNullOrEmpty(result)
                ? "Nothing to do (no Global Volume found, or it already has it)."
                : result.Trim() + "\n\nSave the scene (Ctrl+S).",
            "OK");
    }

    // ---------------- Helpers ----------------

    private static string AddBrightnessToGlobalVolume()
    {
        foreach (Volume volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (volume.isGlobal && volume.GetComponent<BrightnessFromSettings>() == null)
            {
                Undo.AddComponent<BrightnessFromSettings>(volume.gameObject);
                return "\n- Added Brightness From Settings to '" + volume.name + "'.";
            }
        }
        return "";
    }

    private static string AddScenesToBuild()
    {
        var scenes = new List<EditorBuildSettingsScene>();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null)
            scenes.Add(new EditorBuildSettingsScene(MenuScenePath, true));
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) != null)
            scenes.Add(new EditorBuildSettingsScene(GameScenePath, true));

        // Keep any other scenes that were already in the list.
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (existing.path != MenuScenePath && existing.path != GameScenePath)
                scenes.Add(existing);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        return "\n- Build list: MainMenu (0), SampleScene (1).";
    }

    private static void LoadResources()
    {
        uiResources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };

        tmpResources = new TMP_DefaultControls.Resources
        {
            standard = uiResources.standard,
            background = uiResources.background,
            inputField = uiResources.inputField,
            knob = uiResources.knob,
            checkmark = uiResources.checkmark,
            dropdown = uiResources.dropdown,
            mask = uiResources.mask,
        };
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddLayout(GameObject go, float width, float height)
    {
        LayoutElement layout = go.GetComponent<LayoutElement>();
        if (layout == null) layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = CreateRect("Spacer", parent);
        AddLayout(spacer, -1, height);
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
    {
        GameObject go = TMP_DefaultControls.CreateText(tmpResources);
        go.name = name;
        go.transform.SetParent(parent, false);

        TMP_Text tmp = go.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = TextColor;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label, AudioSource uiAudio)
    {
        GameObject go = TMP_DefaultControls.CreateButton(tmpResources);
        go.name = name;
        go.transform.SetParent(parent, false);
        AddLayout(go, 420, 80);

        Image image = go.GetComponent<Image>();
        image.color = ButtonColor;

        TMP_Text text = go.GetComponentInChildren<TMP_Text>();
        text.text = label;
        text.fontSize = 40;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        // Tint the TEXT on hover (warm orange), not the dark background.
        Button button = go.GetComponent<Button>();
        button.targetGraphic = text;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.88f, 0.85f, 0.82f);
        colors.highlightedColor = new Color(1f, 0.72f, 0.48f);
        colors.pressedColor = new Color(0.8f, 0.5f, 0.32f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        button.colors = colors;

        MenuButtonHover hover = go.AddComponent<MenuButtonHover>();
        SetRef(hover, "audioSource", uiAudio);

        return button;
    }

    private static GameObject CreateRow(Transform parent, string label)
    {
        GameObject row = CreateRect(label + " Row", parent);
        AddLayout(row, -1, 64);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TMP_Text text = CreateText("Label", row.transform, label, 34, TextAlignmentOptions.Left);
        AddLayout(text.gameObject, 340, 64);
        return row;
    }

    private static Slider CreateSliderRow(Transform parent, string label)
    {
        GameObject row = CreateRow(parent, label);

        GameObject go = DefaultControls.CreateSlider(uiResources);
        go.name = "Slider";
        go.transform.SetParent(row.transform, false);
        AddLayout(go, 480, 36);

        RectTransform handle = go.transform.Find("Handle Slide Area/Handle") as RectTransform;
        if (handle != null) handle.sizeDelta = new Vector2(34, 0);

        Image fill = go.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
        if (fill != null) fill.color = new Color(1f, 0.72f, 0.48f);

        return go.GetComponent<Slider>();
    }

    private static Toggle CreateToggleRow(Transform parent, string label)
    {
        GameObject row = CreateRow(parent, label);

        GameObject go = DefaultControls.CreateToggle(uiResources);
        go.name = "Toggle";
        go.transform.SetParent(row.transform, false);
        AddLayout(go, 48, 48);

        // Remove the default (old-style) label, the row already has one.
        Transform oldLabel = go.transform.Find("Label");
        if (oldLabel != null) Object.DestroyImmediate(oldLabel.gameObject);

        RectTransform background = go.transform.Find("Background") as RectTransform;
        if (background != null)
        {
            Stretch(background);
            RectTransform check = background.Find("Checkmark") as RectTransform;
            if (check != null)
            {
                Stretch(check);
                check.offsetMin = new Vector2(6, 6);
                check.offsetMax = new Vector2(-6, -6);
            }
        }

        return go.GetComponent<Toggle>();
    }

    private static TMP_Dropdown CreateDropdownRow(Transform parent, string label)
    {
        GameObject row = CreateRow(parent, label);

        GameObject go = TMP_DefaultControls.CreateDropdown(tmpResources);
        go.name = "Dropdown";
        go.transform.SetParent(row.transform, false);
        AddLayout(go, 480, 56);

        TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();
        if (dropdown.captionText != null) dropdown.captionText.fontSize = 28;
        if (dropdown.itemText != null) dropdown.itemText.fontSize = 26;

        // Make the drop-down list taller so the bigger text fits.
        RectTransform template = go.transform.Find("Template") as RectTransform;
        if (template != null) template.sizeDelta = new Vector2(template.sizeDelta.x, 320);

        RectTransform content = go.transform.Find("Template/Viewport/Content") as RectTransform;
        if (content != null) content.sizeDelta = new Vector2(content.sizeDelta.x, 48);

        RectTransform item = go.transform.Find("Template/Viewport/Content/Item") as RectTransform;
        if (item != null) item.sizeDelta = new Vector2(item.sizeDelta.x, 44);

        return dropdown;
    }

    private static void SetRef(Object target, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning("MainMenuBuilder: field '" + fieldName + "' not found on " + target.GetType().Name);
            return;
        }
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
