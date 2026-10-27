using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LevelSelectBuilder
{
    private const string UiRoot = "Assets/_Project/UI/LevelSelect";
    private const string ScenePath = "Assets/_Project/Scenes/StartScene.unity";

    [MenuItem("Tools/Toy Dash/Rebuild Level Select")]
    public static void Build()
    {
        ImportSprites();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.2f, 0.25f, 1f);
        camera.cullingMask = 0;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        GameObject root = new GameObject("LevelSelectUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LevelSelectController));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1680, 945);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject backgroundObject = new GameObject("LevelSelectArtwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(root.transform, false);
        Image background = backgroundObject.GetComponent<Image>();
        background.sprite = Sprite("background.png");
        background.preserveAspect = false;
        background.raycastTarget = false;
        Stretch(background.rectTransform);

        LevelSelectController controller = root.GetComponent<LevelSelectController>();
        CreateInactiveCard("BeachCard", root.transform, Sprite("beach.png"), new Vector2(0.22f, 0.43f), new Vector2(400, 400));
        Button kitchen = CreateSpriteButton("KitchenCard", root.transform, Sprite("kitchen.png"), new Vector2(0.50f, 0.43f), new Vector2(400, 400));
        CreateInactiveCard("ToyRoomCard", root.transform, Sprite("toy-room.png"), new Vector2(0.78f, 0.43f), new Vector2(400, 400));
        Button back = CreateSpriteButton("BackButton", root.transform, Sprite("back.png"), new Vector2(0.13f, 0.105f), new Vector2(360, 120));
        UnityEventTools.AddPersistentListener(kitchen.onClick, controller.OpenKitchen);
        UnityEventTools.AddPersistentListener(back.onClick, controller.Back);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(scene, ScenePath);
        SetBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Toy Dash level select rebuilt successfully.");
    }

    private static void ImportSprites()
    {
        foreach (string file in Directory.GetFiles(UiRoot, "*.png"))
        {
            string path = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
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

    private static Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 normalizedAnchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = normalizedAnchor;
        rect.anchorMax = normalizedAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.68f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        return button;
    }

    private static void CreateInactiveCard(string name, Transform parent, Sprite sprite, Vector2 normalizedAnchor, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = normalizedAnchor;
        rect.anchorMax = normalizedAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

private static void SetBuildScenes()
    {
        string[] paths =
        {
            "Assets/_Project/Scenes/MainMenuScene.unity",
            "Assets/_Project/Scenes/StartScene.unity",
            "Assets/_Project/Scenes/Beach.unity",
            "Assets/_Project/Scenes/Kitchen_Adventure.unity"
        };
        EditorBuildSettings.scenes = Array.ConvertAll(paths, p => new EditorBuildSettingsScene(p, true));
    }
}
