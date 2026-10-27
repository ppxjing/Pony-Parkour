using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class BeachToKitchenStoryPage : MonoBehaviour
{
    private readonly string[] titles =
    {
        "SCENE 1  THE BEACH RACE ENDS",
        "SCENE 2  A NEW CLUE",
        "SCENE 3  THE NEXT RACE",
        "LEVEL 2 — KITCHEN"
    };

    private readonly string[] descriptions =
    {
        "Four toy horses cross the beach finish line.\nWhatever the finishing order, they discover the first glowing star fragment.",
        "The star fragment rises slowly and gives off a soft golden light.\nIts light points towards the distant kitchen.",
        "The four toy horses look towards the kitchen and prepare for the next race.\nThe starlight leads the way from the sunny beach to the warm kitchen.",
        ""
    };

    private readonly string[] narrations =
    {
        "The first star fragment has been found!",
        "The next fragment is waiting in the kitchen!",
        "The race isn't over yet. Let's keep going!",
        ""
    };

    private int index;
    private Text titleText;
    private Text descriptionText;
    private Text narrationText;
    private Text progressText;
    private Text hintText;
    private Action onComplete;

    public static void Show(Action onComplete)
    {
        BeachToKitchenStoryPage existing = FindFirstObjectByType<BeachToKitchenStoryPage>();
        if (existing != null) return;

        GameObject root = new GameObject("Beach To Kitchen Story");
        BeachToKitchenStoryPage page = root.AddComponent<BeachToKitchenStoryPage>();
        page.onComplete = onComplete;
        page.Build();
        page.DisplayCurrentPage();
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

        Image shade = Panel("Night Shade", transform, Vector2.zero, Vector2.one,
            new Color(0.025f, 0.055f, 0.11f, 0.94f));
        shade.raycastTarget = true;
        Button advance = shade.gameObject.AddComponent<Button>();
        advance.transition = Selectable.Transition.None;
        advance.onClick.AddListener(Next);

        Image frame = Panel("Golden Story Frame", transform, new Vector2(0.12f, 0.16f), new Vector2(0.88f, 0.84f),
            new Color(0.96f, 0.68f, 0.23f, 1f));
        Panel("Story Inset", frame.transform, new Vector2(0.018f, 0.03f), new Vector2(0.982f, 0.97f),
            new Color(0.10f, 0.075f, 0.08f, 0.98f));

        titleText = Label("Title", frame.transform, new Vector2(0.07f, 0.72f), new Vector2(0.93f, 0.91f),
            48, TextAnchor.MiddleCenter, new Color(1f, 0.80f, 0.32f));

        descriptionText = Label("Description", frame.transform, new Vector2(0.10f, 0.40f), new Vector2(0.90f, 0.70f),
            29, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.80f));

        narrationText = Label("Narration", frame.transform, new Vector2(0.10f, 0.21f), new Vector2(0.90f, 0.40f),
            36, TextAnchor.MiddleCenter, Color.white);
        narrationText.fontStyle = FontStyle.BoldAndItalic;

        progressText = Label("Progress", frame.transform, new Vector2(0.06f, 0.06f), new Vector2(0.24f, 0.16f),
            19, TextAnchor.MiddleLeft, new Color(0.97f, 0.73f, 0.31f));

        hintText = Label("Continue Hint", frame.transform, new Vector2(0.52f, 0.06f), new Vector2(0.94f, 0.16f),
            19, TextAnchor.MiddleRight, new Color(0.97f, 0.73f, 0.31f));
    }

    private void Next()
    {
        index++;
        if (index < titles.Length)
        {
            DisplayCurrentPage();
            return;
        }

        Action callback = onComplete;
        Destroy(gameObject);
        callback?.Invoke();
    }

    private void DisplayCurrentPage()
    {
        bool levelCard = index == titles.Length - 1;
        titleText.text = titles[index];
        titleText.fontSize = levelCard ? 72 : 48;
        descriptionText.text = descriptions[index];
        narrationText.text = string.IsNullOrEmpty(narrations[index]) ? "" : "“" + narrations[index] + "”";
        progressText.text = (index + 1) + "  /  " + titles.Length;
        hintText.text = levelCard ? "CLICK TO START  ▶" : "CLICK ANYWHERE TO CONTINUE  ▶";
    }

    private static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text Label(string name, Transform parent, Vector2 min, Vector2 max, int size,
        TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }
}
