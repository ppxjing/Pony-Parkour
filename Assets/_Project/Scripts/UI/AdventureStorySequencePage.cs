using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class AdventureStorySequencePage : MonoBehaviour
{
    // 调整这五组锚点即可微调对话框、标题和正文的位置。
    private static readonly Vector2 DialogueMin = new(0.025f, 0.018f), DialogueMax = new(0.975f, 0.325f);
    private static readonly Vector2 TitleMin = new(0.095f, 0.57f), TitleMax = new(0.82f, 0.86f);
    private static readonly Vector2 BodyMin = new(0.10f, 0.16f), BodyMax = new(0.84f, 0.61f);

    public readonly struct Page
    {
        public readonly string title, description, dialogue, imagePath;
        public readonly bool finalCard;
        public Page(string title, string description, string dialogue, bool finalCard = false, string imagePath = null)
        { this.title = title; this.description = description; this.dialogue = dialogue; this.finalCard = finalCard; this.imagePath = imagePath; }
    }

    private Page[] pages; private Action onComplete; private int index;
    private Image storyImage; private Text titleText, bodyText, progressText;

    public static void Show(string objectName, Page[] pages, Action onComplete)
    {
        AdventureStorySequencePage old = FindFirstObjectByType<AdventureStorySequencePage>();
        if (old != null) Destroy(old.gameObject);
        GameObject root = new(objectName);
        AdventureStorySequencePage view = root.AddComponent<AdventureStorySequencePage>();
        view.pages = pages; view.onComplete = onComplete; view.Build(); view.Display();
    }

    private void Build()
    {
        gameObject.AddComponent<RectTransform>();
        Canvas canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 26000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();

        storyImage = Panel("Story Image", transform, Vector2.zero, Vector2.one, Color.white);
        Image clickLayer = Panel("Click Anywhere", transform, Vector2.zero, Vector2.one, new Color(0, 0, 0, .06f));
        clickLayer.raycastTarget = true; Button button = clickLayer.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None; button.onClick.AddListener(Next);

        Image frame = Panel("Ornate Dialogue Frame", transform, DialogueMin, DialogueMax, Color.white);
        frame.sprite = Resources.Load<Sprite>("UI/Story/ornate-dialogue-frame");
        titleText = Label("Shot Title", frame.transform, TitleMin, TitleMax, 31, TextAnchor.MiddleLeft);
        bodyText = Label("Story Text", frame.transform, BodyMin, BodyMax, 25, TextAnchor.MiddleLeft);
        progressText = Label("Page Number", frame.transform, new Vector2(.80f, .12f), new Vector2(.91f, .30f), 18, TextAnchor.MiddleCenter);
    }

    private void Next()
    {
        if (++index < pages.Length) { Display(); return; }
        Action callback = onComplete; Destroy(gameObject); callback?.Invoke();
    }

    private void Display()
    {
        Page page = pages[index];
        Sprite sprite = string.IsNullOrWhiteSpace(page.imagePath) ? null : Resources.Load<Sprite>(page.imagePath);
        storyImage.sprite = sprite; storyImage.color = sprite == null ? new Color(.05f, .06f, .1f, 1) : Color.white;
        titleText.text = page.title; titleText.fontSize = page.finalCard ? 38 : 31;
        bodyText.text = page.description + (string.IsNullOrWhiteSpace(page.dialogue) ? "" : "\n" + page.dialogue);
        bodyText.fontSize = page.finalCard ? 28 : 25; progressText.text = $"{index + 1} / {pages.Length}";
    }

    private static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }

    private static Text Label(string name, Transform parent, Vector2 min, Vector2 max, int size, TextAnchor alignment)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
        Text t = go.GetComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = size;
        t.fontStyle = FontStyle.Bold; t.alignment = alignment; t.color = new Color(.27f, .13f, .045f);
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.raycastTarget = false; return t;
    }
}

public static class KitchenToBedroomStoryPage
{
    public static void Show(Action done) => AdventureStorySequencePage.Show("Kitchen To Bedroom Story", new[]
    {
        new AdventureStorySequencePage.Page("SHOT 1 — THE KITCHEN RACE ENDS", "The four friends cross the kitchen finish line. A second golden fragment rises above the moonlit tables.", "Narration:  “The second star fragment has been found!”", imagePath: "UI/Story/story-09"),
        new AdventureStorySequencePage.Page("SHOT 2 — ONLY ONE REMAINS", "The first and second fragments answer one another. Their joined light points through an open doorway.", "Clover:  “Only one fragment remains!”", imagePath: "UI/Story/story-10"),
        new AdventureStorySequencePage.Page("SHOT 3 — FOLLOW THE LIGHT", "The glowing trail winds along the quiet hotel corridor towards the bedroom.", "Woody:  “The final fragment is waiting for us!”", imagePath: "UI/Story/story-15"),
        new AdventureStorySequencePage.Page("LEVEL 3 — BEDROOM", "THE FINAL RACE", "One last race. Let's find our way home!", true, "UI/Story/story-11")
    }, done);
}

public static class BedroomEndingStoryPage
{
    public static void Show(Action done) => AdventureStorySequencePage.Show("Bedroom Ending Story", new[]
    {
        new AdventureStorySequencePage.Page("SHOT 1 — THE FINAL FINISH LINE", "The four toy horses cross the final finish line and gather in the moonlit bedroom.", "Blaze:  “We did it! That was amazing!”\nBubbles:  “That was the best race ever!”", imagePath: "UI/Story/story-12"),
        new AdventureStorySequencePage.Page("SHOT 2 — THE FINAL STAR FRAGMENT", "The third fragment rises. All three pieces spin beneath the moonlight and form one magical star.", "Clover:  “Look! The star is complete!”\nWoody:  “We found all three pieces!”", imagePath: "UI/Story/story-16"),
        new AdventureStorySequencePage.Page("SHOT 3 — THE MAGIC OF AFTER HOURS", "Golden light sweeps across the room. The teddy bear, blocks and other toys begin to awaken.", "Bubbles:  “Wow! Everything is coming to life!”\nWoody:  “That's the magic of after hours!”", imagePath: "UI/Story/story-11"),
        new AdventureStorySequencePage.Page("SHOT 4 — TIME TO GO HOME", "The night begins to fade. The magical star lights a path back to the toy box.", "Clover:  “Look! It's almost morning!”\nBlaze:  “Then we'd better hurry!”", imagePath: "UI/Story/story-15"),
        new AdventureStorySequencePage.Page("SHOT 5 — JUST ORDINARY TOYS?", "The horses return to their places. Morning light enters the quiet room, as though nothing happened.", "Narration:  “When the world wakes up, the toys go back to sleep.”", imagePath: "UI/Story/story-13"),
        new AdventureStorySequencePage.Page("TOY DASH — THE ADVENTURE NEVER ENDS", "Woody's little star gives one final glow before the picture fades to black.", "Narration:  “But when the lights go out... the adventure begins again.”\nEvery night has a secret adventure.\nTHANK YOU FOR PLAYING!", true, "UI/Story/story-14")
    }, done);
}
