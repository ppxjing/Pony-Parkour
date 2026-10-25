using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuBuilder
{
    private const string UiRoot = "Assets/_Project/UI/MainMenu";
    private const string ScenePath = "Assets/_Project/Scenes/MainMenuScene.unity";
    private static readonly Color Cream = new Color(1f, 0.91f, 0.66f, 1f);
    private static readonly Color Brown = new Color(0.24f, 0.105f, 0.035f, 1f);

    [MenuItem("Tools/Toy Dash/Rebuild Main Menu")]
    public static void Build()
    {
        ImportSprites();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.16f, 0.2f, 1f);
        camera.cullingMask = 0;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        GameObject root = new GameObject("MainMenuUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainMenuController));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image background = CreateImage("Background", root.transform, Sprite("background.png"));
        Stretch(background.rectTransform);
        background.preserveAspect = false;
        background.raycastTarget = false;

        Image vignette = CreateImage("WarmOverlay", root.transform, null);
        Stretch(vignette.rectTransform);
        vignette.color = new Color(0.08f, 0.035f, 0.01f, 0.16f);
        vignette.raycastTarget = false;

        Image logo = CreateImage("ToyDashLogo", root.transform, Sprite("logo.png"));
        SetRect(logo.rectTransform, new Vector2(0f, 1f), new Vector2(500, -250), new Vector2(820, 500));
        logo.preserveAspect = true;
        logo.raycastTarget = false;

        GameObject menu = new GameObject("MenuButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        menu.transform.SetParent(root.transform, false);
        RectTransform menuRect = menu.GetComponent<RectTransform>();
        SetRect(menuRect, new Vector2(1f, 0.5f), new Vector2(-330, -30), new Vector2(560, 650));
        VerticalLayoutGroup layout = menu.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 24;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        MainMenuController controller = root.GetComponent<MainMenuController>();
        Button play = CreateSpriteButton("Play", menu.transform, Sprite("play.png"), new Vector2(540, 180));
        Button levels = CreateSpriteButton("Levels", menu.transform, Sprite("levels.png"), new Vector2(540, 180));
        Button settings = CreateSpriteButton("Settings", menu.transform, Sprite("settings.png"), new Vector2(540, 180));
        UnityEventTools.AddPersistentListener(play.onClick, controller.Play);
        UnityEventTools.AddPersistentListener(levels.onClick, controller.OpenLevels);
        UnityEventTools.AddPersistentListener(settings.onClick, controller.OpenSettings);

        Button quit = CreateTextButton("Quit", menu.transform, "QUIT GAME", new Vector2(260, 62), new Color(0.34f, 0.13f, 0.045f, 0.9f));
        UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);

        GameObject levelsPanel = CreatePanel(root.transform, "LevelsPanel", "CHOOSE A LEVEL");
        Button adventure = CreateTextButton("Adventure", levelsPanel.transform, "KITCHEN ADVENTURE", new Vector2(520, 95), new Color(0.12f, 0.55f, 0.72f, 1f));
        SetRect(adventure.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(520, 95));
        Button playground = CreateTextButton("Playground", levelsPanel.transform, "KITCHEN PLAYGROUND", new Vector2(520, 95), new Color(0.35f, 0.68f, 0.32f, 1f));
        SetRect(playground.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(520, 95));
        Button closeLevels = CreateTextButton("Back", levelsPanel.transform, "BACK", new Vector2(220, 65), Brown);
        SetRect(closeLevels.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(220, 65));
        UnityEventTools.AddPersistentListener(adventure.onClick, controller.LoadAdventure);
        UnityEventTools.AddPersistentListener(playground.onClick, controller.LoadPlayground);
        UnityEventTools.AddPersistentListener(closeLevels.onClick, controller.ClosePanels);

        GameObject settingsPanel = CreatePanel(root.transform, "SettingsPanel", "SETTINGS");
        Text volumeLabel = CreateText("VolumeLabel", settingsPanel.transform, "MASTER VOLUME", 30, Brown);
        SetRect(volumeLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-170, 85), new Vector2(280, 55));
        Slider slider = CreateSlider(settingsPanel.transform);
        SetRect(slider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(125, 85), new Vector2(300, 36));
        Toggle toggle = CreateToggle(settingsPanel.transform);
        SetRect(toggle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -15), new Vector2(420, 60));
        Button closeSettings = CreateTextButton("Back", settingsPanel.transform, "BACK", new Vector2(220, 65), Brown);
        SetRect(closeSettings.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(220, 65));
        UnityEventTools.AddPersistentListener(closeSettings.onClick, controller.ClosePanels);

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("levelsPanel").objectReferenceValue = levelsPanel;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("volumeSlider").objectReferenceValue = slider;
        so.FindProperty("fullscreenToggle").objectReferenceValue = toggle;
        so.ApplyModifiedPropertiesWithoutUndo();
        levelsPanel.SetActive(false);
        settingsPanel.SetActive(false);

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(null);

        EditorSceneManager.SaveScene(scene, ScenePath);
        SetBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Toy Dash main menu rebuilt successfully.");
    }

    private static void ImportSprites()
    {
        foreach (string path in Directory.GetFiles(UiRoot, "*.png"))
        {
            TextureImporter importer = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiRoot}/{name}");

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        return image;
    }

    private static Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 size)
    {
        Image image = CreateImage(name + "Button", parent, sprite);
        image.preserveAspect = true;
        image.rectTransform.sizeDelta = size;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.96f, 0.78f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private static Button CreateTextButton(string name, Transform parent, string label, Vector2 size, Color color)
    {
        Image image = CreateImage(name, parent, null);
        image.color = color;
        image.rectTransform.sizeDelta = size;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text text = CreateText("Label", image.transform, label, 28, Cream);
        Stretch(text.rectTransform);
        return button;
    }

    private static GameObject CreatePanel(Transform parent, string name, string heading)
    {
        GameObject shade = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shade.transform.SetParent(parent, false);
        Stretch(shade.GetComponent<RectTransform>());
        shade.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.01f, 0.72f);

        GameObject card = new GameObject("WoodenCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(shade.transform, false);
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 530));
        card.GetComponent<Image>().color = new Color(0.96f, 0.78f, 0.46f, 0.98f);
        Text title = CreateText("Heading", card.transform, heading, 48, Brown);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -75), new Vector2(650, 75));
        return shade;
    }

    private static Slider CreateSlider(Transform parent)
    {
        GameObject go = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        Image bg = CreateImage("Background", go.transform, null);
        Stretch(bg.rectTransform);
        bg.color = new Color(0.36f, 0.16f, 0.055f, 1f);
        Image fill = CreateImage("Fill", go.transform, null);
        SetRect(fill.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(250, 20));
        fill.rectTransform.pivot = new Vector2(0, 0.5f);
        fill.color = new Color(0.18f, 0.65f, 0.75f, 1f);
        Image handle = CreateImage("Handle", go.transform, null);
        SetRect(handle.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 46));
        handle.color = Cream;
        Slider slider = go.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0.8f;
        return slider;
    }

    private static Toggle CreateToggle(Transform parent)
    {
        GameObject go = new GameObject("FullscreenToggle", typeof(RectTransform), typeof(Toggle));
        go.transform.SetParent(parent, false);
        Image box = CreateImage("Box", go.transform, null);
        SetRect(box.rectTransform, new Vector2(0f, 0.5f), new Vector2(30, 0), new Vector2(48, 48));
        box.color = Brown;
        Image check = CreateImage("Checkmark", box.transform, null);
        SetRect(check.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28));
        check.color = new Color(0.23f, 0.78f, 0.41f, 1f);
        Text label = CreateText("Label", go.transform, "FULLSCREEN", 30, Brown);
        SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(220, 0), new Vector2(300, 55));
        Toggle toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = true;
        return toggle;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = size;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetBuildScenes()
    {
        string[] paths = {
            "Assets/_Project/Scenes/MainMenuScene.unity",
            "Assets/_Project/Scenes/Kitchen_Adventure.unity",
            "Assets/_Project/Scenes/Kitchen_Playground.unity"
        };
        EditorBuildSettings.scenes = Array.ConvertAll(paths, p => new EditorBuildSettingsScene(p, true));
    }
}
