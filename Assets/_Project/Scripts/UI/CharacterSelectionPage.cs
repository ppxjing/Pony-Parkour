using System;
using Adventure.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CharacterSelectionPage : MonoBehaviour
{
    public const string SelectionKey = "selected-player-horse";
    private Action onSelected;

    private readonly CharacterEntry[] entries =
    {
        new CharacterEntry("FLOWER HORSE", "flowerhorse", "UI/Characters/flowerhorse"),
        new CharacterEntry("FLOWER HORSE B", "flowerhorse_b", "UI/Characters/flowerhorse-b"),
        new CharacterEntry("HORSE", "horse", "UI/Characters/horse")
    };

    public static void Show(Action onSelected)
    {
        CharacterSelectionPage existing = FindFirstObjectByType<CharacterSelectionPage>();
        if (existing != null) return;
        GameObject root = new GameObject("Character Selection Page");
        CharacterSelectionPage page = root.AddComponent<CharacterSelectionPage>();
        page.onSelected = onSelected;
        page.Build();
    }

    private void Build()
    {
        gameObject.AddComponent<RectTransform>();
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        Image background = ImageObject("Background", transform);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one);
        background.sprite = Resources.Load<Sprite>("UI/character-select-bg");
        background.preserveAspect = false;

        Image shade = ImageObject("Card Shade", transform);
        Stretch(shade.rectTransform, new Vector2(0.055f, 0.18f), new Vector2(0.945f, 0.66f));
        shade.color = new Color(0.15f, 0.055f, 0.018f, 0.28f);

        for (int i = 0; i < entries.Length; i++)
        {
            CharacterEntry entry = entries[i];
            float x = 0.08f + i * 0.28f;
            Button card = Card(entry, new Vector2(x, 0.20f), new Vector2(x + 0.26f, 0.63f));
            card.onClick.AddListener(() => Select(entry.id));
        }

        Text hint = Label("Hint", transform, new Vector2(0.25f, 0.105f), new Vector2(0.75f, 0.17f), 28);
        hint.text = "CHOOSE A HORSE TO CONTINUE";
        hint.color = new Color(1f, 0.91f, 0.70f);
    }

    private Button Card(CharacterEntry entry, Vector2 min, Vector2 max)
    {
        Image panel = ImageObject(entry.label, transform);
        Stretch(panel.rectTransform, min, max);
        panel.color = new Color(1f, 0.76f, 0.32f, 0.98f);

        Button button = panel.gameObject.AddComponent<Button>();
        button.targetGraphic = panel;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.93f, 0.67f, 1f);
        colors.pressedColor = new Color(0.80f, 0.62f, 0.32f, 1f);
        button.colors = colors;

        Image portrait = ImageObject("Portrait", panel.transform);
        Stretch(portrait.rectTransform, new Vector2(0.035f, 0.19f), new Vector2(0.965f, 0.965f));
        portrait.sprite = Resources.Load<Sprite>(entry.portraitPath);
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        Text name = Label("Name", panel.transform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.18f), 30);
        name.text = entry.label;
        name.color = new Color(0.24f, 0.075f, 0.015f);
        name.raycastTarget = false;
        return button;
    }

    private void Select(string id)
    {
        PlayerPrefs.SetString(SelectionKey, id);
        PlayerPrefs.Save();
        Action callback = onSelected;
        Destroy(gameObject);
        callback?.Invoke();
    }

    private static Image ImageObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    private static Text Label(string name, Transform parent, Vector2 min, Vector2 max, int size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Stretch(rect, min, max);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = size;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private readonly struct CharacterEntry
    {
        public readonly string label;
        public readonly string id;
        public readonly string portraitPath;
        public CharacterEntry(string label, string id, string portraitPath)
        {
            this.label = label;
            this.id = id;
            this.portraitPath = portraitPath;
        }
    }
}

public sealed class SelectedPlayerCharacter : MonoBehaviour
{
    // Per-character tuning.
    private static (float height, float yOffset) GetTuning(string id)
    {
        switch (id)
        {
            case "horse":
                // Only the "horse" model: smaller, and lowered independently.
                return (1.1f, 0f);
            default:
                // flowerhorse and flowerhorse_b keep the current look.
                return (1.5f, -0.5f);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= ApplyToScene;
        SceneManager.sceneLoaded += ApplyToScene;
    }

    private static void ApplyToScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenuScene" || scene.name == "StartScene") return;
        ThirdPersonMotor motor = FindFirstObjectByType<ThirdPersonMotor>();
        if (motor == null) return;

        string id = PlayerPrefs.GetString(CharacterSelectionPage.SelectionKey, "horse");
        GameObject source = Resources.Load<GameObject>("PlayerCharacterPrefabs/" + id);
        if (source == null) source = Resources.Load<GameObject>("PlayerCharacters/" + id + "/" + id);
        if (source == null)
        {
            Debug.LogError("Selected player model could not be loaded: " + id);
            return;
        }

        Transform visuals = motor.transform.Find("Visuals");
        Transform visualParent = visuals != null ? visuals : motor.transform;

        Transform existing = visualParent.Find("Selected Player Visual");
        if (existing == null)
            existing = motor.transform.Find("Selected Player Visual");
        if (existing != null) Destroy(existing.gameObject);

        if (visuals != null)
            foreach (Renderer renderer in visuals.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;

        // ThirdPersonMotor turns the Visuals node toward the movement direction.
        // Parent the selected model to that node so the model follows steering.
        GameObject visual = Instantiate(source, visualParent);
        visual.name = "Selected Player Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var (targetHeight, yOffset) = GetTuning(id);

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            // Keep skin sizing proportional to the player body's authored scale.
            float bodyScale = Mathf.Abs(motor.transform.localScale.y);
            float scale = bounds.size.y > 0.001f ? targetHeight * bodyScale / bounds.size.y : 1f;
            visual.transform.localScale = Vector3.one * scale;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 center = visualParent.InverseTransformPoint(bounds.center);
            Vector3 bottom = visualParent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            visual.transform.localPosition = new Vector3(-center.x, -bottom.y + yOffset, -center.z);
        }

        foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>(true)) childCollider.enabled = false;
        foreach (Rigidbody childBody in visual.GetComponentsInChildren<Rigidbody>(true)) childBody.isKinematic = true;
    }
}