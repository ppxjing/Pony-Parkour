using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LevelSelectController : MonoBehaviour
{
private void Awake()
    {
        WireCard("BeachCard", "Beach", OpenBeach);
        WireCard("KitchenCard", "Kitchen_Adventure", OpenKitchen);
        WireCard("ToyRoomCard", "Bedroom", OpenBedroom);
    }

    public void OpenBeach() => Load("Beach");
    public void OpenKitchen() => Load("Kitchen_Adventure");
    public void OpenBedroom() => Load("Bedroom");
    public void Back() => SceneManager.LoadScene("MainMenuScene");

    private static void WireCard(string objectName, string sceneName, UnityEngine.Events.UnityAction action)
    {
        GameObject card = GameObject.Find(objectName);
        if (card == null) return;
        Image image = card.GetComponent<Image>();
        if (image == null) return;
        image.raycastTarget = true;

        Button button = card.GetComponent<Button>();
        if (button == null)
        {
            button = card.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.94f, 0.70f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            button.colors = colors;
            button.onClick.AddListener(action);
        }
        button.interactable = LevelStarProgress.IsUnlocked(sceneName);
        if (!button.interactable)
        {
            GameObject notice = new GameObject("Star Requirement", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            notice.transform.SetParent(card.transform, false);
            RectTransform rect = notice.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.17f, 0.06f);
            rect.anchorMax = new Vector2(0.83f, 0.29f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image shade = notice.GetComponent<Image>();
            shade.color = new Color(0.10f, 0.04f, 0.01f, 0.94f);
            shade.raycastTarget = false;
            GameObject label = new GameObject("Lock Reason", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(notice.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 6f);
            labelRect.offsetMax = new Vector2(-10f, -6f);
            Text text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 25;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 25;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.85f, 0.3f);
            text.raycastTarget = false;
            string previous = LevelStarProgress.Prerequisite(sceneName) == "Beach" ? "BEACH" : "KITCHEN";
            text.text = "LOCKED\n" + previous + " STARS: 80%";
        }
    }

    private static void Load(string sceneName)
    {
        if (!LevelStarProgress.IsUnlocked(sceneName)) return;
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogError("Scene '" + sceneName + "' is not included in Build Settings.");
    }
}
