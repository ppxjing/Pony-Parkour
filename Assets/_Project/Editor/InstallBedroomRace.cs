using System.Collections.Generic;
using Adventure.AI;
using Adventure.Player;
using Cinemachine;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class InstallBedroomRace
{
    private const string ScenePath = "Assets/_Project/Scenes/Bedroom.unity";
    private const string PlayerPrefab = "Assets/_prefab/玩家/PlayerRig.prefab";
    private const string AIPrefab = "Assets/_prefab/AI/PF_NavigationTestAI.prefab";
    private const string RootName = "Bedroom Race Gameplay";
    private const string RouteName = "Bedroom Race Route (MOVE THESE POINTS)";
    private const string LinkRootName = "Bedroom Route Shortcuts And Loop";

    [MenuItem("Tools/Adventure/Install or Rebuild Bedroom Race")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        Bounds floorBounds = FindFloorBounds();
        GameObject root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        GameObject navigation = new GameObject("Bedroom Navigation (REBAKE AFTER EDITING)");
        navigation.transform.SetParent(root.transform);
        NavMeshSurface surface = navigation.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.layerMask = ~0;
        surface.BuildNavMesh();

        List<Transform> route = CreateRoute(root.transform, floorBounds);
        if (route.Count < 10)
            throw new System.InvalidOperationException("Bedroom NavMesh produced too few route points. Check Floor_Bedroom_Grp and rebake.");
        InstallRouteLinks(root.transform, route);

        Vector3 forward = Vector3.ProjectOnPlane(route[1].position - route[0].position, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);

        GameObject playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        GameObject aiAsset = AssetDatabase.LoadAssetAtPath<GameObject>(AIPrefab);
        if (playerAsset == null || aiAsset == null)
            throw new System.InvalidOperationException("PlayerRig or PF_NavigationTestAI prefab is missing.");

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerAsset, scene);
        player.name = "Bedroom Player";
        player.transform.SetParent(root.transform);
        player.transform.SetPositionAndRotation(route[0].position - right * 1.2f, facing);

        ThirdPersonMotor motor = player.GetComponentInChildren<ThirdPersonMotor>(true);
        PlayerInputReader input = player.GetComponentInChildren<PlayerInputReader>(true);
        ThirdPersonOrbitCamera orbit = player.GetComponentInChildren<ThirdPersonOrbitCamera>(true);
        Transform cameraTarget = FindChild(player.transform, "CameraTarget");
        if (motor == null || input == null || orbit == null || cameraTarget == null)
            throw new System.InvalidOperationException("PlayerRig camera references are incomplete.");
        orbit.Configure(cameraTarget, motor.transform, input);

        GameObject ai = (GameObject)PrefabUtility.InstantiatePrefab(aiAsset, scene);
        ai.name = "Bedroom AI Source";
        ai.transform.SetParent(root.transform);
        NavMeshAgent agent = ai.GetComponent<NavMeshAgent>();
        Vector3 aiStart = route[0].position + right * 1.2f;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (NavMesh.SamplePosition(aiStart, out NavMeshHit aiHit, 3f, filter)) aiStart = aiHit.position;
        ai.transform.SetPositionAndRotation(aiStart, facing);

        NavMeshLoopPatrol patrol = ai.GetComponent<NavMeshLoopPatrol>();
        AssignTransforms(patrol, "waypoints", route);

        KitchenRaceManager manager = new GameObject("Bedroom Race Manager").AddComponent<KitchenRaceManager>();
        manager.transform.SetParent(root.transform);
        SerializedObject managerSO = new SerializedObject(manager);
        managerSO.FindProperty("player").objectReferenceValue = player.transform;
        AssignTransforms(managerSO, "route", route);
        managerSO.FindProperty("targetLaps").intValue = 2;
        managerSO.FindProperty("checkpointRadius").floatValue = 3f;
        managerSO.ApplyModifiedPropertiesWithoutUndo();

        RaceStartCountdown countdown = new GameObject("RaceStartCountdown").AddComponent<RaceStartCountdown>();
        countdown.transform.SetParent(root.transform);
        AssignCountdownSprites(countdown);

        Camera playerCamera = player.GetComponentInChildren<Camera>(true);
        foreach (Camera sceneCamera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (sceneCamera != playerCamera) sceneCamera.gameObject.SetActive(false);
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            playerCamera.enabled = true;
            if (playerCamera.GetComponent<CinemachineBrain>() == null)
                playerCamera.gameObject.AddComponent<CinemachineBrain>();
            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
            orbit.SnapToTarget();
        }

        Selection.activeGameObject = route[0].parent.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Bedroom race installed with {route.Count} editable route points, player follow camera, three AI racers and a closed loop.");
    }

    [MenuItem("Tools/Adventure/Rebake Bedroom NavMesh Only")]
    public static void RebakeOnly()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject navigation = GameObject.Find("Bedroom Navigation (REBAKE AFTER EDITING)");
        NavMeshSurface surface = navigation != null ? navigation.GetComponent<NavMeshSurface>() : null;
        if (surface == null) throw new System.InvalidOperationException("Install the Bedroom race first.");
        surface.BuildNavMesh();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Adventure/Refresh Bedroom Route Shortcuts And Loop")]
    public static void RefreshLinks()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject holder = GameObject.Find(RouteName);
        GameObject root = GameObject.Find(RootName);
        if (holder == null || root == null) throw new System.InvalidOperationException("Install the Bedroom race first.");
        var route = new List<Transform>();
        for (int i = 0; i < holder.transform.childCount; i++) route.Add(holder.transform.GetChild(i));
        SnapRouteToNavMesh(route);
        InstallRouteLinks(root.transform, route);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static Bounds FindFloorBounds()
    {
        GameObject floor = GameObject.Find("Floor_Bedroom_Grp");
        if (floor == null) floor = GameObject.Find("Room_Bedroom_Grp");
        Renderer[] renderers = floor != null ? floor.GetComponentsInChildren<Renderer>(true) : null;
        if (renderers == null || renderers.Length == 0)
            throw new System.InvalidOperationException("Floor_Bedroom_Grp has no renderers.");
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static List<Transform> CreateRoute(Transform root, Bounds bounds)
    {
        GameObject holder = new GameObject(RouteName);
        holder.transform.SetParent(root);
        float x = Mathf.Max(4f, bounds.extents.x * 0.72f);
        float z = Mathf.Max(4f, bounds.extents.z * 0.72f);
        Vector3 center = new Vector3(bounds.center.x, bounds.max.y + 0.25f, bounds.center.z);
        var route = new List<Transform>();
        const int desiredCount = 20;
        for (int i = 0; i < desiredCount; i++)
        {
            float angle = (-135f + i * 360f / desiredCount) * Mathf.Deg2Rad;
            Vector3 desired = center + new Vector3(Mathf.Cos(angle) * x, 0f, Mathf.Sin(angle) * z);
            if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, Mathf.Max(x, z) * 0.38f, NavMesh.AllAreas)) continue;
            if (route.Count > 0 && Vector3.Distance(route[route.Count - 1].position, hit.position) < 0.8f) continue;
            GameObject point = new GameObject("BedroomRoute_" + (route.Count + 1).ToString("00"));
            point.transform.SetParent(holder.transform);
            point.transform.position = hit.position;
            AIPatrolWaypoint marker = point.AddComponent<AIPatrolWaypoint>();
            SerializedObject markerSO = new SerializedObject(marker);
            markerSO.FindProperty("waitDuration").floatValue = 0f;
            markerSO.FindProperty("arrivalDistance").floatValue = 0.5f;
            markerSO.FindProperty("navMeshSampleDistance").floatValue = 1.5f;
            markerSO.ApplyModifiedPropertiesWithoutUndo();
            route.Add(point.transform);
        }
        return route;
    }

    private static void SnapRouteToNavMesh(List<Transform> route)
    {
        NavMeshAgent agent = Object.FindFirstObjectByType<NavMeshAgent>();
        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agent != null ? agent.agentTypeID : 0,
            areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas
        };
        foreach (Transform point in route)
            if (NavMesh.SamplePosition(point.position, out NavMeshHit hit, 5f, filter)) point.position = hit.position;
    }

    private static void InstallRouteLinks(Transform root, List<Transform> route)
    {
        Transform old = root.Find(LinkRootName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject holder = new GameObject(LinkRootName);
        holder.transform.SetParent(root, false);
        for (int i = 0; i < route.Count; i++)
        {
            Transform start = route[i];
            Transform end = route[(i + 1) % route.Count];
            var path = new NavMeshPath();
            bool complete = NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete;
            float direct = Vector3.Distance(start.position, end.position);
            float pathLength = PathLength(path);
            bool blocked = NavMesh.Raycast(start.position, end.position, out _, NavMesh.AllAreas);
            bool finalLoop = i == route.Count - 1;
            bool detour = complete && pathLength > direct * 1.4f + 0.5f;
            if (!finalLoop && !blocked && complete && !detour) continue;
            if (!finalLoop && direct > 9f) continue;
            GameObject go = new GameObject(finalLoop ? "BedroomLoop_Last_to_First" : $"BedroomShortcut_{i + 1:00}_to_{i + 2:00}");
            go.transform.SetParent(holder.transform, false);
            NavMeshLink link = go.AddComponent<NavMeshLink>();
            link.startTransform = start;
            link.endTransform = end;
            link.width = 0.9f;
            link.bidirectional = false;
            link.costModifier = Mathf.Max(0.1f, direct * 0.8f);
            link.autoUpdate = true;
        }
    }

    private static float PathLength(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2) return float.PositiveInfinity;
        float result = 0f;
        for (int i = 1; i < path.corners.Length; i++) result += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return result;
    }

    private static void AssignTransforms(NavMeshLoopPatrol patrol, string property, List<Transform> route)
    {
        SerializedObject so = new SerializedObject(patrol);
        AssignTransforms(so, property, route);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignTransforms(SerializedObject so, string property, List<Transform> route)
    {
        SerializedProperty list = so.FindProperty(property);
        list.arraySize = route.Count;
        for (int i = 0; i < route.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = route[i];
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
        return null;
    }

    private static void AssignCountdownSprites(RaceStartCountdown countdown)
    {
        string[] guids =
        {
            "887c9e1e052fc434c99f6c3313bbcc06", "772c708e62ca755499cfd6f75240f3cb",
            "4c719929d5d069142bb537bff83113b3", "4d559f916fd15354c89691de7292205e"
        };
        string[] properties = { "threeSprite", "twoSprite", "oneSprite", "goSprite" };
        SerializedObject so = new SerializedObject(countdown);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) { so.FindProperty(properties[i]).objectReferenceValue = sprite; break; }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Used by automated/editor verification. It does not modify the scene.
    public static void Validate()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject root = GameObject.Find(RootName);
        if (root == null) throw new System.InvalidOperationException("Bedroom race root is missing.");

        Transform routeRoot = root.transform.Find(RouteName);
        int routeCount = routeRoot == null ? 0 : routeRoot.childCount;
        if (routeCount < 10) throw new System.InvalidOperationException($"Bedroom only has {routeCount} route points.");

        Transform links = root.transform.Find(LinkRootName);
        if (links == null || links.Find("BedroomLoop_Last_to_First") == null)
            throw new System.InvalidOperationException("Bedroom route is not connected from last point to first point.");

        NavMeshSurface surface = root.GetComponentInChildren<NavMeshSurface>(true);
        if (surface == null || surface.navMeshData == null)
            throw new System.InvalidOperationException("Bedroom NavMesh has not been baked.");
        if (root.GetComponentInChildren<ThirdPersonOrbitCamera>(true) == null)
            throw new System.InvalidOperationException("Bedroom player follow camera is missing.");
        if (root.GetComponentInChildren<KitchenRaceManager>(true) == null)
            throw new System.InvalidOperationException("Bedroom race manager is missing.");
        if (root.GetComponentInChildren<NavMeshLoopPatrol>(true) == null)
            throw new System.InvalidOperationException("Bedroom AI navigation is missing.");

        Debug.Log($"Bedroom race validation passed: {routeCount} editable route points, baked NavMesh, loop link, player camera and AI race manager.");
    }
}
