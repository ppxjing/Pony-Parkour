using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LevelSelectController : MonoBehaviour
{
    private void Awake()
    {
        WireCard("BeachCard", OpenBeach);
        WireCard("ToyRoomCard", OpenBedroom);
        WireCard("KitchenCard", OpenKitchen);
    }

    public void OpenBeach() => Load("Beach");
    public void OpenKitchen() => SceneManager.LoadScene("Kitchen_Adventure");
    public void OpenBedroom() => Load("Bedroom");
    public void Back() => SceneManager.LoadScene("MainMenuScene");

    private static void WireCard(string objectName, UnityEngine.Events.UnityAction action)
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
    }

    private static void Load(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogError("Scene '" + sceneName + "' is not included in Build Settings.");
    }
}
