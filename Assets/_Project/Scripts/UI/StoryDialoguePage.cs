using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class StoryDialoguePage : MonoBehaviour
{
    private readonly string[] speakers = { "WOODY", "BUBBLES", "WOODY" };
    private readonly string[] lines =
    {
        "Huh? What's happening?",
        "Woody! You're awake!",
        "Wait... we can move?"
    };

    private int index;
    private Text speakerText;
    private Text dialogueText;
    private Text progressText;
    private Action onComplete;

    public static void Show(Action onComplete)
    {
        StoryDialoguePage existing = FindFirstObjectByType<StoryDialoguePage>();
        if (existing != null) return;
        GameObject root = new GameObject("Story Dialogue Page");
        StoryDialoguePage page = root.AddComponent<StoryDialoguePage>();
        page.onComplete = onComplete;
        page.Build();
        page.DisplayCurrentLine();
    }

    private void Build()
    {
        gameObject.AddComponent<RectTransform>();
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        Image shade = Panel("Warm Bedroom Shade", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.075f, 0.025f, 0.012f, 0.88f));

        // The full-screen button makes mouse/touch anywhere advance the story.
        Button advance = shade.gameObject.AddComponent<Button>();
        advance.transition = Selectable.Transition.None;
        advance.onClick.AddListener(Next);

        Text chapter = Label("Chapter", transform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.93f), 54, TextAnchor.MiddleCenter);
        chapter.text = "THE RACE BEGINS";
        chapter.color = new Color(1f, 0.78f, 0.28f);

        Text direction = Label("Scene Direction", transform, new Vector2(0.13f, 0.59f), new Vector2(0.87f, 0.75f), 27, TextAnchor.MiddleCenter);
        direction.text = "A warm beam of golden light slips into the quiet toy room.\nThe star on Woody's body begins to glow, and the little wooden horse gently stirs.";
        direction.color = new Color(1f, 0.91f, 0.75f);

        Image dialoguePanel = Panel("Wooden Dialogue Panel", transform, new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.46f),
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0.96f, 0.69f, 0.30f, 0.98f));

        Image inner = Panel("Dialogue Inset", dialoguePanel.transform, new Vector2(0.025f, 0.07f), new Vector2(0.975f, 0.88f),
            Vector2.zero, Vector2.zero, new Color(0.20f, 0.075f, 0.025f, 0.96f));

        speakerText = Label("Speaker", dialoguePanel.transform, new Vector2(0.06f, 0.79f), new Vector2(0.36f, 0.98f), 31, TextAnchor.MiddleLeft);
        speakerText.color = new Color(0.25f, 0.08f, 0.02f);

        dialogueText = Label("Dialogue", inner.transform, new Vector2(0.06f, 0.17f), new Vector2(0.94f, 0.88f), 39, TextAnchor.MiddleLeft);
        dialogueText.color = Color.white;

        Text hint = Label("Continue Hint", dialoguePanel.transform, new Vector2(0.55f, 0.01f), new Vector2(0.94f, 0.15f), 22, TextAnchor.MiddleRight);
        hint.text = "CLICK ANYWHERE TO CONTINUE  ▶";
        hint.color = new Color(0.27f, 0.09f, 0.02f);

        progressText = Label("Progress", dialoguePanel.transform, new Vector2(0.06f, 0.01f), new Vector2(0.25f, 0.15f), 21, TextAnchor.MiddleLeft);
        progressText.color = new Color(0.27f, 0.09f, 0.02f);
    }

    private void Next()
    {
        index++;
        if (index < lines.Length)
        {
            DisplayCurrentLine();
            return;
        }
        Action callback = onComplete;
        Destroy(gameObject);
        callback?.Invoke();
    }

    private void DisplayCurrentLine()
    {
        speakerText.text = speakers[index];
        dialogueText.text = "“" + lines[index] + "”";
        progressText.text = (index + 1) + "  /  " + lines.Length;
        speakerText.color = speakers[index] == "WOODY"
            ? new Color(0.65f, 0.09f, 0.035f)
            : new Color(0.02f, 0.37f, 0.75f);
    }

    private static Image Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        Image image = go.GetComponent<Image>(); image.color = color;
        return image;
    }

    private static Text Label(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
