using UnityEngine;
using UnityEngine.UI;

public sealed class LevelCountdownHud : MonoBehaviour
{
    private CountdownClockGraphic clock;
    private Text secondsText;
    private int displayedSeconds = -1;
    private bool urgent;
    private static readonly Color NormalColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color WarningColor = new Color(1f, 0.22f, 0.18f);
    public const float WarningSeconds = 10f;

    public static LevelCountdownHud Create(Transform parent)
    {
        GameObject panel = new GameObject("Level Countdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(28f, -116f);
        rect.sizeDelta = new Vector2(240f, 196f);
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.12f, 0.055f, 0.015f, 0.88f);
        background.raycastTarget = false;
        LevelCountdownHud hud = panel.AddComponent<LevelCountdownHud>();

        GameObject face = new GameObject("Clock Progress", typeof(RectTransform), typeof(CanvasRenderer), typeof(CountdownClockGraphic));
        face.transform.SetParent(panel.transform, false);
        RectTransform faceRect = face.GetComponent<RectTransform>();
        faceRect.anchorMin = faceRect.anchorMax = new Vector2(0.5f, 1f);
        faceRect.anchoredPosition = new Vector2(0f, -82f);
        faceRect.sizeDelta = new Vector2(144f, 144f);
        hud.clock = face.GetComponent<CountdownClockGraphic>();
        hud.clock.raycastTarget = false;
        hud.clock.color = NormalColor;
        hud.secondsText = CreateText(face.transform, "Remaining Seconds", 34, Vector2.zero, new Vector2(80f, 48f));
        hud.secondsText.color = NormalColor;
        hud.secondsText.resizeTextForBestFit = true;
        hud.secondsText.resizeTextMinSize = 12;
        hud.secondsText.resizeTextMaxSize = 34;
        Text label = CreateText(panel.transform, "Time Label", 21, new Vector2(0f, -72f), new Vector2(220f, 32f));
        label.text = "SECONDS LEFT";
        label.color = new Color(0.98f, 0.88f, 0.65f);
        return hud;
    }

    public void SetTime(float remaining, float total)
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
        if (seconds != displayedSeconds)
        {
            displayedSeconds = seconds;
            secondsText.text = seconds.ToString();
        }
        bool warning = remaining <= WarningSeconds;
        if (warning != urgent)
        {
            urgent = warning;
            clock.color = secondsText.color = urgent ? WarningColor : NormalColor;
        }
        clock.SetFraction(total > 0f ? remaining / total : 0f);
    }

    private static Text CreateText(Transform parent, string name, int size, Vector2 position, Vector2 dimensions)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }
}