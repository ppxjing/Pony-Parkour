using System;
using System.Collections.Generic;
using System.Linq;
using Adventure.AI;
using Adventure.Player;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

[DefaultExecutionOrder(-500)]
public sealed class KitchenRaceManager : MonoBehaviour
{
    [Serializable]
    private struct RacerSetup
    {
        public string name;
        public Color color;
        public float speedMultiplier;
        public float startLane;
        public Vector3 modelPosition;
        public Vector3 modelRotation;
        public float modelScale;

        public RacerSetup(string name, Color color, float speedMultiplier, float startLane)
        {
            this.name = name;
            this.color = color;
            this.speedMultiplier = speedMultiplier;
            this.startLane = startLane;
            modelPosition = new Vector3(-0.1f, 0.3f, 0.01f);
            modelRotation = new Vector3(-90f, -90f, 0f);
            modelScale = 0.6f;
        }
    }

    [SerializeField] private Transform player;
    [SerializeField] private List<NavMeshLoopPatrol> opponents = new List<NavMeshLoopPatrol>();
    [SerializeField] private List<Transform> route = new List<Transform>();
    [SerializeField, Min(1)] private int targetLaps = 2;
    [SerializeField, Min(0.5f)] private float checkpointRadius = 2.5f;

    private int playerNextWaypoint = 1;
    private int playerLaps;
    private bool finished;
    private Text lapText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoader()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Kitchen_Adventure") return;
        if (FindFirstObjectByType<KitchenRaceManager>() == null)
            new GameObject("Kitchen Race Manager").AddComponent<KitchenRaceManager>();
    }

    private void Start()
    {
        AutoConfigureIfNeeded();
        CreateLapDisplay();
        UpdateLapText();
    }

    private void AutoConfigureIfNeeded()
    {
        if (player == null)
        {
            ThirdPersonMotor motor = FindFirstObjectByType<ThirdPersonMotor>();
            if (motor != null) player = motor.transform;
        }

        NavMeshLoopPatrol source = FindFirstObjectByType<NavMeshLoopPatrol>();
        if (source == null) return;
        if (route.Count == 0) route.AddRange(source.Waypoints);
        opponents.Clear();

        RacerSetup[] setups =
        {
            new RacerSetup("Elephant", Color.white, 1f, 0f),
            new RacerSetup("Giraff", Color.white, 0.94f, 1.15f),
            new RacerSetup("Rabbit", Color.white, 1.06f, 1f)
        };
        string[] modelPaths =
        {
            "RacerModels/elephant/elephant",
            "RacerModels/giraff/giraff",
            "RacerModels/rabbit/r"
        };
        NavMeshAgent sourceAgent = source.GetComponent<NavMeshAgent>();
        float baseSpeed = sourceAgent.speed;
        Vector3 raceForward = route.Count > 1
            ? Vector3.ProjectOnPlane(route[1].position - route[0].position, Vector3.up).normalized
            : source.transform.forward;
        if (raceForward.sqrMagnitude < 0.01f) raceForward = source.transform.forward;
        Vector3 raceRight = Vector3.Cross(Vector3.up, raceForward).normalized;
        Vector3 gridCenter = source.transform.position;

        for (int i = 0; i < setups.Length; i++)
        {
            RacerSetup setup = setups[i];
            GameObject racer = i == 0 ? source.gameObject : Instantiate(source.gameObject, gridCenter, Quaternion.LookRotation(raceForward));
            racer.name = setup.name;
            NavMeshAgent agent = racer.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            Vector3 desired = gridCenter + raceRight * setup.startLane;
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 3f, filter))
                agent.Warp(hit.position);
            racer.transform.rotation = Quaternion.LookRotation(raceForward, Vector3.up);
            agent.speed = baseSpeed * setup.speedMultiplier;
            agent.avoidancePriority = 40 + i * 10;
            NavMeshLoopPatrol patrol = racer.GetComponent<NavMeshLoopPatrol>();
            GameObject model = Resources.Load<GameObject>(modelPaths[i]);
            if (model == null) Debug.LogError("Could not load AI racer model: " + modelPaths[i]);
            InstallRacerVisual(racer, model, setup);
            opponents.Add(patrol);
        }

        // Waypoint 0 is the starting grid. Sending offset racers back to it can make one
        // of them take a path in the opposite direction, so all racers begin at Link 1.
        foreach (NavMeshLoopPatrol racer in opponents)
            racer.BeginRaceAtWaypoint(route.Count > 1 ? 1 : 0);
    }

    private static void InstallRacerVisual(GameObject racer, GameObject modelPrefab, RacerSetup setup)
    {
        if (modelPrefab == null) return;
        Transform oldModel = racer.transform.Find("Model");
        if (oldModel != null) Destroy(oldModel.gameObject);

        Transform existing = racer.transform.Find("Race Visual");
        if (existing != null) Destroy(existing.gameObject);

        GameObject visual = Instantiate(modelPrefab, racer.transform);
        visual.name = "Race Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(setup.modelRotation);
        visual.transform.localScale = Vector3.one;

        // The imported animals have different source dimensions. Normalize them to
        // the same racing height and place their feet on the NavMesh agent's floor.
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float scale = bounds.size.y > 0.001f ? 1.65f / bounds.size.y : 1f;
            visual.transform.localScale = Vector3.one * scale * setup.modelScale;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 center = racer.transform.InverseTransformPoint(bounds.center);
            Vector3 bottom = racer.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            visual.transform.localPosition = setup.modelPosition + new Vector3(-center.x, -bottom.y, -center.z);
        }

        foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>(true)) childCollider.enabled = false;
        foreach (Rigidbody childBody in visual.GetComponentsInChildren<Rigidbody>(true)) childBody.isKinematic = true;
    }

    private void Update()
    {
        if (finished || player == null || route.Count < 2) return;
        Transform next = route[Mathf.Clamp(playerNextWaypoint, 0, route.Count - 1)];
        if (next == null || Vector3.Distance(player.position, next.position) > checkpointRadius) return;

        playerNextWaypoint++;
        if (playerNextWaypoint < route.Count) return;
        playerNextWaypoint = 0;
        playerLaps++;
        UpdateLapText();
        if (playerLaps >= targetLaps)
        {
            FinishRace();
            return;
        }
        // Require crossing the start waypoint before beginning the next lap.
        playerNextWaypoint = 0;
    }

    private void LateUpdate()
    {
        if (finished || playerNextWaypoint != 0 || route.Count == 0) return;
        if (Vector3.Distance(player.position, route[0].position) <= checkpointRadius)
            playerNextWaypoint = 1;
    }

    private void FinishRace()
    {
        finished = true;
        var results = new List<ResultEntry>
        {
            new ResultEntry("PLAYER", PlayerProgress(), true)
        };
        foreach (NavMeshLoopPatrol opponent in opponents)
            if (opponent != null)
                results.Add(new ResultEntry(opponent.gameObject.name, AIProgress(opponent), false));
        results = results.OrderByDescending(x => x.progress).ToList();

        ThirdPersonMotor motor = player.GetComponent<ThirdPersonMotor>();
        if (motor != null) motor.enabled = false;
        PlayerInputReader input = player.GetComponent<PlayerInputReader>();
        if (input != null) input.enabled = false;

        ShowResults(results);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private float PlayerProgress()
    {
        return playerLaps * route.Count + Mathf.Clamp(playerNextWaypoint, 0, route.Count - 1) + SegmentFraction(player.position, playerNextWaypoint);
    }

    private float AIProgress(NavMeshLoopPatrol ai)
    {
        int index = Mathf.Clamp(ai.CurrentWaypointIndex, 0, route.Count - 1);
        return ai.CompletedLoops * route.Count + index + SegmentFraction(ai.transform.position, index);
    }

    private float SegmentFraction(Vector3 position, int nextIndex)
    {
        if (route.Count < 2) return 0f;
        int previousIndex = (nextIndex - 1 + route.Count) % route.Count;
        Transform previous = route[previousIndex];
        Transform next = route[Mathf.Clamp(nextIndex, 0, route.Count - 1)];
        if (previous == null || next == null) return 0f;
        float length = Vector3.Distance(previous.position, next.position);
        return length < 0.01f ? 0f : Mathf.Clamp01(1f - Vector3.Distance(position, next.position) / length);
    }

    private void CreateLapDisplay()
    {
        Canvas canvas = NewCanvas("Race HUD", 5000);
        GameObject panel = new GameObject("Lap Counter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -28f);
        rect.sizeDelta = new Vector2(280f, 72f);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.055f, 0.015f, 0.78f);
        lapText = NewText(panel.transform, 34, TextAnchor.MiddleCenter);
    }

    private void UpdateLapText()
    {
        if (lapText != null) lapText.text = "LAP  " + Mathf.Min(playerLaps + 1, targetLaps) + " / " + targetLaps;
    }

    private void ShowResults(List<ResultEntry> results)
    {
        Canvas canvas = NewCanvas("Race Results", 10000);
        GameObject shade = new GameObject("Shade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shade.transform.SetParent(canvas.transform, false);
        RectTransform shadeRect = shade.GetComponent<RectTransform>();
        shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one;
        shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
        shade.GetComponent<Image>().color = new Color(0.03f, 0.012f, 0f, 0.78f);

        GameObject board = new GameObject("Ranking Board", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        board.transform.SetParent(canvas.transform, false);
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.anchorMin = boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(1400f, 788f);
        Image boardImage = board.GetComponent<Image>();
        boardImage.sprite = Resources.Load<Sprite>("UI/ranking-podium");
        boardImage.preserveAspect = true;
        boardImage.color = Color.white;

        // Coordinates match the four dashed name boxes in ranking-podium.png.
        Vector2[] positions =
        {
            new Vector2(-113f, -168f), // 1st, center red podium
            new Vector2(-441f, -209f), // 2nd, left blue podium
            new Vector2(202f, -210f),  // 3rd, right green podium
            new Vector2(492f, -247f)   // 4th, far-right orange podium
        };
        Vector2[] sizes =
        {
            new Vector2(245f, 58f), new Vector2(210f, 54f),
            new Vector2(210f, 54f), new Vector2(190f, 50f)
        };
        int[] fontSizes = { 31, 27, 27, 24 };
        for (int i = 0; i < Mathf.Min(4, results.Count); i++)
        {
            Text nameText = NewPlacedText(board.transform, "Rank " + (i + 1) + " Name", positions[i], sizes[i], fontSizes[i]);
            nameText.text = results[i].name + (results[i].isPlayer ? " ★" : "");
        }
    }

    private static Text NewPlacedText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.20f, 0.075f, 0.02f, 1f);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 15;
        text.resizeTextMaxSize = fontSize;
        return text;
    }

    private static Canvas NewCanvas(string name, int order)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static Text NewText(Transform parent, int size, TextAnchor alignment)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20f, 15f); rect.offsetMax = new Vector2(-20f, -15f);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private readonly struct ResultEntry
    {
        public readonly string name;
        public readonly float progress;
        public readonly bool isPlayer;
        public ResultEntry(string name, float progress, bool isPlayer) { this.name = name; this.progress = progress; this.isPlayer = isPlayer; }
    }
}

public sealed class GamePauseMenu : MonoBehaviour
{
    private GameObject pausePanel;
    private float previousTimeScale = 1f;
    private bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterForScenes()
    {
        SceneManager.sceneLoaded -= CreateForGameplayScene;
        SceneManager.sceneLoaded += CreateForGameplayScene;
    }

    private static void CreateForGameplayScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenuScene" || scene.name == "StartScene") return;
        if (FindFirstObjectByType<GamePauseMenu>() != null) return;
        new GameObject("Game Pause Menu").AddComponent<GamePauseMenu>().Build();
    }

    private void Build()
    {
        EnsureEventSystem();
        Canvas canvas = NewPauseCanvas();

        Button settings = NewButton(canvas.transform, "Settings Button", "⚙  SETTINGS", new Vector2(0.84f, 0.88f), new Vector2(0.98f, 0.97f), 27);
        settings.onClick.AddListener(Open);

        pausePanel = new GameObject("Pause Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pausePanel.transform.SetParent(canvas.transform, false);
        RectTransform full = pausePanel.GetComponent<RectTransform>();
        full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one;
        full.offsetMin = full.offsetMax = Vector2.zero;
        pausePanel.GetComponent<Image>().color = new Color(0.035f, 0.012f, 0.005f, 0.84f);

        GameObject board = new GameObject("Settings Board", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        board.transform.SetParent(pausePanel.transform, false);
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.anchorMin = boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(620f, 520f);
        board.GetComponent<Image>().color = new Color(0.91f, 0.56f, 0.18f, 0.99f);

        Text title = PauseText(board.transform, "PAUSED", new Vector2(0.08f, 0.69f), new Vector2(0.92f, 0.93f), 56);
        title.color = new Color(0.22f, 0.065f, 0.012f);

        Button resume = NewButton(board.transform, "Resume", "▶  RESUME", new Vector2(0.18f, 0.40f), new Vector2(0.82f, 0.59f), 32);
        resume.onClick.AddListener(Resume);
        Button mainMenu = NewButton(board.transform, "Main Menu", "⌂  MAIN MENU", new Vector2(0.18f, 0.15f), new Vector2(0.82f, 0.34f), 30);
        mainMenu.onClick.AddListener(ReturnToMainMenu);

        pausePanel.SetActive(false);
    }

    private void Open()
    {
        if (paused) return;
        paused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Resume()
    {
        if (!paused) return;
        paused = false;
        pausePanel.SetActive(false);
        Time.timeScale = previousTimeScale;
        if (SceneManager.GetActiveScene().name == "Kitchen_Adventure")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ReturnToMainMenu()
    {
        paused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OnDestroy()
    {
        if (paused) Time.timeScale = 1f;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Canvas NewPauseCanvas()
    {
        GameObject go = new GameObject("Pause UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static Button NewButton(Transform parent, string name, string label, Vector2 min, Vector2 max, int size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>(); image.color = new Color(0.18f, 0.48f, 0.33f, 0.98f);
        Button button = go.GetComponent<Button>(); button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.28f, 0.70f, 0.48f, 1f);
        colors.pressedColor = new Color(0.11f, 0.30f, 0.20f, 1f);
        button.colors = colors;
        Text text = PauseText(go.transform, label, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), size);
        text.color = Color.white;
        return button;
    }

    private static Text PauseText(Transform parent, string value, Vector2 min, Vector2 max, int size)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size; text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter;
        text.text = value; text.raycastTarget = false;
        return text;
    }
}
