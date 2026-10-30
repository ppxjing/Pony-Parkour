using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-10000)]
public sealed class RaceStartCountdown : MonoBehaviour
{
    [SerializeField, Min(1)] private int countdownSeconds = 3;
    [SerializeField, Min(0f)] private float goDisplaySeconds = 0.65f;
    [SerializeField] private Sprite threeSprite;
    [SerializeField] private Sprite twoSprite;
    [SerializeField] private Sprite oneSprite;
    [SerializeField] private Sprite goSprite;

    private float previousTimeScale;
    private float previousFixedDeltaTime;
    private Canvas overlayCanvas;
    private Image countdownImage;
    private bool released;

    public bool IsCountingDown => !released;

    private void Awake()
    {
        previousTimeScale = Time.timeScale;
        previousFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = 0f;
        CreateOverlay();
    }

    private void Start()
    {
        StartCoroutine(RunCountdown());
    }

    private IEnumerator RunCountdown()
    {
        for (int number = countdownSeconds; number >= 1; number--)
        {
            Show(number);
            yield return new WaitForSecondsRealtime(1f);
        }

        ReleaseRace();
        Show(0);
        yield return new WaitForSecondsRealtime(goDisplaySeconds);

        if (overlayCanvas != null)
            Destroy(overlayCanvas.gameObject);
        Destroy(gameObject);
    }

    private void CreateOverlay()
    {
        GameObject canvasObject = new GameObject("RaceCountdownOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject shadeObject = new GameObject("Shade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shadeObject.transform.SetParent(canvasObject.transform, false);
        RectTransform shadeRect = shadeObject.GetComponent<RectTransform>();
        shadeRect.anchorMin = Vector2.zero;
        shadeRect.anchorMax = Vector2.one;
        shadeRect.offsetMin = Vector2.zero;
        shadeRect.offsetMax = Vector2.zero;
        shadeObject.GetComponent<Image>().color = new Color(0.06f, 0.025f, 0.01f, 0.30f);

        GameObject imageObject = new GameObject("CountdownBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = new Vector2(520f, 520f);
        countdownImage = imageObject.GetComponent<Image>();
        countdownImage.preserveAspect = true;
        countdownImage.raycastTarget = false;
        Show(countdownSeconds);
    }

    private void Show(int number)
    {
        if (countdownImage == null) return;
        countdownImage.sprite = number == 3 ? threeSprite : number == 2 ? twoSprite : number == 1 ? oneSprite : goSprite;
        RectTransform rect = countdownImage.rectTransform;
        rect.sizeDelta = number == 0 ? new Vector2(760f, 430f) : new Vector2(520f, 520f);
        countdownImage.enabled = countdownImage.sprite != null;
    }

    private void ReleaseRace()
    {
        if (released) return;
        released = true;
        float raceTimeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        GamePauseMenu pauseMenu = FindFirstObjectByType<GamePauseMenu>();
        // A settings menu opened during 3-2-1 must keep both the race and its timer paused.
        if (pauseMenu != null && pauseMenu.IsPaused)
            pauseMenu.SetResumeTimeScale(raceTimeScale);
        else
            Time.timeScale = raceTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime;
    }

    private void OnDestroy()
    {
        ReleaseRace();
    }
}
