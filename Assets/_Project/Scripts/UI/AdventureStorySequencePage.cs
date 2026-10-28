using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A reusable click-anywhere story page used between race levels and after the finale.</summary>
public sealed class AdventureStorySequencePage : MonoBehaviour
{
    public readonly struct Page
    {
        public readonly string title;
        public readonly string description;
        public readonly string dialogue;
        public readonly bool finalCard;

        public Page(string title, string description, string dialogue, bool finalCard = false)
        {
            this.title = title;
            this.description = description;
            this.dialogue = dialogue;
            this.finalCard = finalCard;
        }
    }

    private Page[] pages;
    private Action onComplete;
    private int index;
    private Text titleText;
    private Text descriptionText;
    private Text dialogueText;
    private Text progressText;
    private Text hintText;

    public static void Show(string objectName, Page[] pages, Action onComplete)
    {
        AdventureStorySequencePage existing = FindFirstObjectByType<AdventureStorySequencePage>();
        if (existing != null) Destroy(existing.gameObject);

        GameObject root = new GameObject(objectName);
        AdventureStorySequencePage page = root.AddComponent<AdventureStorySequencePage>();
        page.pages = pages;
        page.onComplete = onComplete;
        page.Build();
        page.Display();
    }

    private void Build()
    {
        gameObject.AddComponent<RectTransform>();
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 26000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        Image shade = Panel("Story Shade", transform, Vector2.zero, Vector2.one,
            new Color(0.025f, 0.035f, 0.075f, 0.96f));
        shade.raycastTarget = true;
        Button advance = shade.gameObject.AddComponent<Button>();
        advance.transition = Selectable.Transition.None;
        advance.onClick.AddListener(Next);

        Image frame = Panel("Golden Story Frame", transform, new Vector2(0.10f, 0.12f), new Vector2(0.90f, 0.88f),
            new Color(0.97f, 0.69f, 0.22f, 1f));
        Panel("Story Inset", frame.transform, new Vector2(0.016f, 0.025f), new Vector2(0.984f, 0.975f),
            new Color(0.095f, 0.065f, 0.075f, 0.985f));

        titleText = Label("Title", frame.transform, new Vector2(0.06f, 0.73f), new Vector2(0.94f, 0.92f),
            48, TextAnchor.MiddleCenter, new Color(1f, 0.81f, 0.34f));
        descriptionText = Label("Description", frame.transform, new Vector2(0.09f, 0.39f), new Vector2(0.91f, 0.72f),
            28, TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.82f));
        dialogueText = Label("Dialogue", frame.transform, new Vector2(0.09f, 0.18f), new Vector2(0.91f, 0.40f),
            31, TextAnchor.MiddleCenter, Color.white);
        dialogueText.fontStyle = FontStyle.BoldAndItalic;
        progressText = Label("Progress", frame.transform, new Vector2(0.05f, 0.05f), new Vector2(0.25f, 0.14f),
            18, TextAnchor.MiddleLeft, new Color(0.97f, 0.73f, 0.31f));
        hintText = Label("Continue Hint", frame.transform, new Vector2(0.50f, 0.05f), new Vector2(0.95f, 0.14f),
            18, TextAnchor.MiddleRight, new Color(0.97f, 0.73f, 0.31f));
    }

    private void Next()
    {
        index++;
        if (index < pages.Length)
        {
            Display();
            return;
        }

        Action callback = onComplete;
        Destroy(gameObject);
        callback?.Invoke();
    }

    private void Display()
    {
        Page page = pages[index];
        titleText.text = page.title;
        titleText.fontSize = page.finalCard ? 68 : 48;
        descriptionText.text = page.description;
        dialogueText.text = page.dialogue;
        progressText.text = (index + 1) + "  /  " + pages.Length;
        hintText.text = page.finalCard ? "CLICK TO CONTINUE  ▶" : "CLICK ANYWHERE TO CONTINUE  ▶";
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

public static class KitchenToBedroomStoryPage
{
    public static void Show(Action onComplete)
    {
        AdventureStorySequencePage.Show("Kitchen To Bedroom Story", new[]
        {
            new AdventureStorySequencePage.Page(
                "SCENE 1 — THE KITCHEN RACE ENDS",
                "Four toy horses cross the kitchen finish line. As the race ends, the second star fragment slowly appears in the air, glowing with warm golden light.",
                "Narration:  “The second star fragment has been found!”"),
            new AdventureStorySequencePage.Page(
                "SCENE 2 — THE FINAL CLUE",
                "The first and second fragments answer one another with a gentle glow. Their light joins into a sparkling trail pointing towards the bedroom.",
                "Narration:  “Only one fragment remains!”"),
            new AdventureStorySequencePage.Page(
                "SCENE 3 — THE FINAL DESTINATION",
                "The camera follows the starlight through the kitchen and along the hallway. Warm sunlight shines through a half-open bedroom door as the trail disappears inside.",
                "Narration:  “The final fragment is waiting in the bedroom!”"),
            new AdventureStorySequencePage.Page(
                "SCENE 4 — ONE LAST RACE",
                "The four toy horses look towards the bedroom, then run forward together. The camera pulls away and the picture fades.",
                "Narration:  “One last race. Let's find our way home!”"),
            new AdventureStorySequencePage.Page(
                "LEVEL 3 — BEDROOM",
                "THE FINAL RACE",
                "",
                true)
        }, onComplete);
    }
}

public static class BedroomEndingStoryPage
{
    public static void Show(Action onComplete)
    {
        AdventureStorySequencePage.Show("Bedroom Ending Story", new[]
        {
            new AdventureStorySequencePage.Page(
                "SHOT 1 — THE FINAL FINISH LINE",
                "Four toy horses cross the bedroom finish line and slowly come to a stop. They look at one another as the camera settles on Woody.",
                "Blaze:  “What a race! That was our best one yet!”\nWoody:  “We made it!”"),
            new AdventureStorySequencePage.Page(
                "SHOT 2 — THE LAST STAR",
                "Warm golden light appears in the bedroom. The third fragment rises while the first two fly from Woody's side. All three circle closer together.",
                "Bubbles:  “Look! The last piece!”\nClover:  “The star is finally complete!”"),
            new AdventureStorySequencePage.Page(
                "SHOT 3 — THE MAGIC RETURNS",
                "The fragments form a complete golden star. Its gentle light fills the bedroom, making the blocks, toy train and teddy bear sparkle before returning to Woody's saddle.",
                "Woody:  “We did it... We brought the magic back!”\n♪ Gentle magical chime"),
            new AdventureStorySequencePage.Page(
                "SHOT 4 — THE WAY HOME",
                "As the glow fades, Woody recognises the toy shelves, blocks and teddy bear by the window. This is where their adventures began, and the four friends stand together again.",
                "Woody:  “This is where our adventures began.”\nBlaze:  “And what an adventure it was!”\nBubbles:  “So... are we racing again tomorrow?”"),
            new AdventureStorySequencePage.Page(
                "SHOT 5 — A NEW BEGINNING",
                "The four horses look out of the window in the warm sunlight. The golden star on Woody's saddle glimmers as the camera slowly pulls back and fades to black.",
                "Clover:  “Of course. The adventure never ends.”\nWoody:  “Next time, let's race together!”"),
            new AdventureStorySequencePage.Page(
                "TOY DASH",
                "The best adventures are the ones we share.",
                "THANK YOU FOR PLAYING!",
                true)
        }, onComplete);
    }
}
