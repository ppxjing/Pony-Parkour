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
    [SerializeField, Range(1f, 2f)] private float npcSpeedScale = 1.2f;
    
    // Legacy serialized field retained for existing scenes and installation tools.
#pragma warning disable CS0414
    [SerializeField, HideInInspector] private float checkpointRadius = 2.5f;
#pragma warning restore CS0414

    [Header("Checkpoint Star")]
    [SerializeField, Min(0.05f)] private float starScale = 0.68f;
    [SerializeField, Min(0f)] private float starHoverHeight = 0.7f;
    [SerializeField, Min(0f)] private float starBobHeight = 0.1f;
    [SerializeField] private float starIdleRotationSpeed = 120f;
    [SerializeField, Min(0.05f)] private float starCollectDuration = 0.8f;
    [SerializeField, Min(0f)] private float starCollectRise = 1.25f;
    [SerializeField] private float starCollectRotationSpeed = 540f;

    private int playerNextWaypoint = 1;
    private int playerLaps;
    private bool finished;
    
    private Text routeText;
    
    private CheckpointStarPickup routeMarker;
    private CharacterController playerController;
    private ThirdPersonMotor playerMotor;
    private PlayerDeath playerDeath;
private Text lapText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoader()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Kitchen_Adventure" && scene.name != "Beach" && scene.name != "Bedroom") return;
        if (FindFirstObjectByType<KitchenRaceManager>() == null)
            new GameObject("Kitchen Race Manager").AddComponent<KitchenRaceManager>();
    }

private void Start()
    {
        AutoConfigureIfNeeded();
        targetLaps = 2;
        CreateLapDisplay();
        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
            playerMotor = player.GetComponent<ThirdPersonMotor>();
            playerDeath = player.GetComponent<PlayerDeath>();
        }
        UpdateLapText();
        UpdateRouteGuidance();
    }

    private void AutoConfigureIfNeeded()
    {
        ThirdPersonMotor playerMotor = FindFirstObjectByType<ThirdPersonMotor>();
        if (playerMotor != null) player = playerMotor.transform;

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
        float baseAcceleration = sourceAgent.acceleration;
        
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
            EnsureAIObstacleHitbox(racer, agent);
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            Vector3 desired = gridCenter + raceRight * setup.startLane;
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 3f, filter))
                agent.Warp(hit.position);
            racer.transform.rotation = Quaternion.LookRotation(raceForward, Vector3.up);
            agent.speed = baseSpeed * setup.speedMultiplier * npcSpeedScale;
            agent.acceleration = baseAcceleration * npcSpeedScale;
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
            // Bounds are world-space: retain the racer root scale when normalizing models.
            float bodyScale = Mathf.Abs(racer.transform.lossyScale.y);
            float scale = bounds.size.y > 0.001f ? 1.65f * bodyScale / bounds.size.y : 1f;
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

private static void EnsureAIObstacleHitbox(GameObject racer, NavMeshAgent agent)
    {
        RacerCollisionBody collisionBody = racer.GetComponent<RacerCollisionBody>();
        if (collisionBody == null) collisionBody = racer.AddComponent<RacerCollisionBody>();
        collisionBody.ConfigureCollision();
    }


    private void LateUpdate()
    {
        if (finished || Time.timeScale <= 0f || player == null || route.Count < 2 ||
            playerMotor == null || !playerMotor.isActiveAndEnabled ||
            (playerDeath != null && !playerDeath.IsAlive)) return;

        // All player movement has run this frame. Only the visible star's collider
        // touching the live player capsule can advance progress, including the lap gate.
        if (routeMarker == null || !routeMarker.IsTouching(playerController)) return;
        routeMarker.Collect();
        routeMarker = null;

        playerNextWaypoint++;
        if (playerNextWaypoint >= route.Count)
        {
            playerNextWaypoint = 0;
            playerLaps++;
            UpdateLapText();
            if (playerLaps >= targetLaps)
            {
                FinishRace();
                return;
            }
        }
        UpdateRouteGuidance();
    }

public bool RespawnPlayerAtLastCheckpoint(ThirdPersonMotor motor)
    {
        if (motor == null || !TryGetPlayerRespawnPose(out Vector3 position, out Quaternion rotation)) return false;
        motor.TeleportAndReset(position, rotation);
        return true;
    }

    public bool TryGetPlayerRespawnPose(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (finished || route.Count == 0) return false;

        int checkpointIndex = (playerNextWaypoint - 1 + route.Count) % route.Count;
        Transform checkpoint = route[checkpointIndex];
        if (checkpoint == null) return false;

        int nextIndex = (checkpointIndex + 1) % route.Count;
        Transform next = route[nextIndex];
        Vector3 forward = next != null
            ? Vector3.ProjectOnPlane(next.position - checkpoint.position, Vector3.up)
            : checkpoint.forward;
        rotation = forward.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(forward, Vector3.up)
            : checkpoint.rotation;

        position = checkpoint.position;
        return true;
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
        if (routeMarker != null) Destroy(routeMarker.gameObject);

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

        GameObject routePanel = new GameObject("Next Checkpoint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        routePanel.transform.SetParent(canvas.transform, false);
        RectTransform routeRect = routePanel.GetComponent<RectTransform>();
        routeRect.anchorMin = routeRect.anchorMax = new Vector2(0.5f, 1f);
        routeRect.pivot = new Vector2(0.5f, 1f);
        routeRect.anchoredPosition = new Vector2(0f, -108f);
        routeRect.sizeDelta = new Vector2(360f, 58f);
        routePanel.GetComponent<Image>().color = new Color(0.68f, 0.25f, 0.02f, 0.84f);
        routeText = NewText(routePanel.transform, 25, TextAnchor.MiddleCenter);
    }

    private void UpdateLapText()
    {
        if (lapText != null) lapText.text = "LAP  " + Mathf.Min(playerLaps + 1, targetLaps) + " / " + targetLaps;
    }

    private void CreateRouteMarker(Transform waypoint)
    {
        Material material = Resources.Load<Material>("Materials/CheckpointStar");
        if (material == null)
        {
            Debug.LogError("Checkpoint star material is missing: Resources/Materials/CheckpointStar", this);
            return;
        }

        GameObject marker = new GameObject("Current Race Checkpoint Star");
        // Keep generated markers owned by this race, with world scale independent of it.
        marker.transform.SetParent(transform, true);
        marker.transform.localScale = new Vector3(
            starScale / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x)),
            starScale / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y)),
            starScale / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.z)));
        routeMarker = marker.AddComponent<CheckpointStarPickup>();
        routeMarker.Initialize(waypoint, CreateStarMesh(), material, starHoverHeight,
            starBobHeight, starIdleRotationSpeed, starCollectDuration,
            starCollectRise, starCollectRotationSpeed);
    }
private static Mesh CreateStarMesh()
    {
        const int pointCount = 10;
        const float outerRadius = 0.9f;
        const float innerRadius = 0.42f;
        const float halfDepth = 0.32f;

        Vector3[] ring = new Vector3[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            float angle = Mathf.PI * 0.5f + i * Mathf.PI * 2f / pointCount;
            ring[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        // Each triangle owns its vertices so RecalculateNormals creates crisp,
        // gem-like facets instead of smoothing the whole star into a flat shape.
        Vector3[] vertices = new Vector3[pointCount * 6];
        int[] triangles = new int[pointCount * 6];

        for (int i = 0; i < pointCount; i++)
        {
            int next = (i + 1) % pointCount;
            int front = i * 3;
            vertices[front] = new Vector3(0f, 0f, halfDepth);
            vertices[front + 1] = ring[i];
            vertices[front + 2] = ring[next];
            triangles[front] = front;
            triangles[front + 1] = front + 1;
            triangles[front + 2] = front + 2;

            int back = pointCount * 3 + i * 3;
            vertices[back] = new Vector3(0f, 0f, -halfDepth);
            vertices[back + 1] = ring[next];
            vertices[back + 2] = ring[i];
            triangles[back] = back;
            triangles[back + 1] = back + 1;
            triangles[back + 2] = back + 2;
        }

        Mesh mesh = new Mesh { name = "Faceted 3D Checkpoint Star" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }


private void UpdateRouteGuidance()
    {
        if (route.Count == 0) return;

        int targetIndex = Mathf.Clamp(playerNextWaypoint, 0, route.Count - 1);
        Transform target = route[targetIndex];
        if (target == null) return;

        if (routeText != null)
        {
            routeText.text = targetIndex == 0
                ? "NEXT: LAP GATE"
                : "NEXT CHECKPOINT  " + (targetIndex + 1).ToString("00") + " / " + route.Count.ToString("00");
        }

        if (routeMarker == null) CreateRouteMarker(target);
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
            new Vector2(-113f, -168f),
            new Vector2(-441f, -209f),
            new Vector2(202f, -210f),
            new Vector2(492f, -247f)
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

        string currentScene = SceneManager.GetActiveScene().name;
        string nextLabel = currentScene == "Beach" ? "NEXT: KITCHEN" :
            currentScene == "Kitchen_Adventure" ? "NEXT: BEDROOM" : "ENDING";

        Button replayButton = NewResultButton(board.transform, "Replay Button", "REPLAY", new Vector2(-330f, -440f));
        replayButton.onClick.AddListener(ReplayCurrentLevel);

        Button menuButton = NewResultButton(board.transform, "Main Menu Button", "MAIN MENU", new Vector2(0f, -440f));
        menuButton.onClick.AddListener(ReturnToMainMenu);

        Button nextButton = NewResultButton(board.transform, "Next Level Button", nextLabel, new Vector2(330f, -440f));
        nextButton.onClick.AddListener(LoadNextLevel);
    }

private void LoadNextLevel()
    {
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "Beach")
        {
            BeachToKitchenStoryPage.Show(() => SceneManager.LoadScene("Kitchen_Adventure"));
            return;
        }

        if (currentScene == "Kitchen_Adventure")
        {
            KitchenToBedroomStoryPage.Show(() => SceneManager.LoadScene("Bedroom"));
            return;
        }

        if (currentScene == "Bedroom")
        {
            BedroomEndingStoryPage.Show(() => SceneManager.LoadScene("MainMenuScene"));
            return;
        }

        SceneManager.LoadScene("MainMenuScene");
    }

private void ReplayCurrentLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

private static Button NewResultButton(Transform parent, string name, string label, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(280f, 78f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.96f, 0.77f, 0.48f, 1f);

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.30f, 0.105f, 0.025f, 0.95f);
        outline.effectDistance = new Vector2(5f, -5f);
        outline.useGraphicAlpha = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.90f, 0.55f, 1f);
        colors.pressedColor = new Color(0.84f, 0.57f, 0.27f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = NewText(go.transform, 27, TextAnchor.MiddleCenter);
        text.text = label;
        text.color = new Color(0.25f, 0.075f, 0.02f, 1f);
        text.raycastTarget = false;
        return button;
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
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
        if (SceneManager.GetActiveScene().name == "Kitchen_Adventure" ||
            SceneManager.GetActiveScene().name == "Beach" ||
            SceneManager.GetActiveScene().name == "Bedroom")
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

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.96f, 0.77f, 0.48f, 1f);

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.30f, 0.105f, 0.025f, 0.95f);
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.90f, 0.55f, 1f);
        colors.pressedColor = new Color(0.84f, 0.57f, 0.27f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = PauseText(go.transform, label, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), size);
        text.color = new Color(0.25f, 0.075f, 0.02f, 1f);
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
