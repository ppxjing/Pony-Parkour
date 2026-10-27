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

/// <summary>
/// Installs an independent copy of the Kitchen race gameplay in Beach.
/// The route is intentionally made from editable scene transforms so level
/// designers can move the points in Scene view and rebake the Beach NavMesh.
/// </summary>
public static class InstallBeachRace
{
    private const string BeachScene = "Assets/_Project/Scenes/Beach.unity";
    private const string PlayerPrefab = "Assets/_prefab/玩家/PlayerRig.prefab";
    private const string AIPrefab = "Assets/_prefab/AI/PF_NavigationTestAI.prefab";
    private const string RootName = "Beach Race Gameplay";
    private const string RouteName = "Beach Race Route (MOVE THESE POINTS)";
    private const string LinkRootName = "Beach Route Shortcuts And Loop";

    [InitializeOnLoadMethod]
    private static void UpgradeOpenBeachScene()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || active.name != "Beach") return;
            GameObject route = GameObject.Find(RouteName);
            if (route != null && route.transform.childCount >= 16)
            {
                NavMeshAgent sceneAgent = Object.FindFirstObjectByType<NavMeshAgent>();
                var filter = new NavMeshQueryFilter
                {
                    agentTypeID = sceneAgent != null ? sceneAgent.agentTypeID : 0,
                    areaMask = sceneAgent != null ? sceneAgent.areaMask : NavMesh.AllAreas
                };
                bool hasInvalidPoint = false;
                for (int i = 0; i < route.transform.childCount; i++)
                    if (!NavMesh.SamplePosition(route.transform.GetChild(i).position, out _, 1.25f, filter))
                        hasInvalidPoint = true;
                if (GameObject.Find(LinkRootName) == null || hasInvalidPoint)
                    InstallRouteLinksAndSave(active, route.transform);
                return;
            }
            Run();
        };
    }

    [MenuItem("Tools/Adventure/Install or Rebuild Beach Race")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(BeachScene, OpenSceneMode.Single);
        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        Bounds floorBounds = FindBeachBounds();
        GameObject root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        GameObject navigation = new GameObject("Beach Navigation (REBAKE AFTER EDITING)");
        navigation.transform.SetParent(root.transform);
        NavMeshSurface surface = navigation.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.layerMask = ~0;
        surface.BuildNavMesh();

        List<Transform> route = CreateEditableRoute(root.transform, floorBounds);
        if (route.Count < 2)
            throw new System.InvalidOperationException("Beach NavMesh produced fewer than two route points. Check Beach_Floor renderers and rebake.");

        InstallRouteLinks(root.transform, route);

        Vector3 forward = Vector3.ProjectOnPlane(route[1].position - route[0].position, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);

        GameObject playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        GameObject aiAsset = AssetDatabase.LoadAssetAtPath<GameObject>(AIPrefab);
        if (playerAsset == null || aiAsset == null)
            throw new System.InvalidOperationException("PlayerRig or PF_NavigationTestAI prefab is missing.");

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerAsset, scene);
        player.name = "Beach Player";
        player.transform.SetParent(root.transform);
        player.transform.SetPositionAndRotation(route[0].position - right * 1.2f, facing);

        ThirdPersonMotor playerMotor = player.GetComponentInChildren<ThirdPersonMotor>(true);
        PlayerInputReader playerInput = player.GetComponentInChildren<PlayerInputReader>(true);
        ThirdPersonOrbitCamera orbitCamera = player.GetComponentInChildren<ThirdPersonOrbitCamera>(true);
        Transform cameraTarget = FindChildNamed(player.transform, "CameraTarget");
        if (playerMotor == null || playerInput == null || orbitCamera == null || cameraTarget == null)
            throw new System.InvalidOperationException("PlayerRig camera references are incomplete.");
        orbitCamera.Configure(cameraTarget, playerMotor.transform, playerInput);

        GameObject ai = (GameObject)PrefabUtility.InstantiatePrefab(aiAsset, scene);
        ai.name = "Beach AI Source";
        ai.transform.SetParent(root.transform);
        NavMeshAgent agent = ai.GetComponent<NavMeshAgent>();
        Vector3 aiStart = route[0].position + right * 1.2f;
        if (NavMesh.SamplePosition(aiStart, out NavMeshHit aiHit, 3f, NavMesh.AllAreas)) aiStart = aiHit.position;
        ai.transform.SetPositionAndRotation(aiStart, facing);

        NavMeshLoopPatrol patrol = ai.GetComponent<NavMeshLoopPatrol>();
        SerializedObject patrolSO = new SerializedObject(patrol);
        SerializedProperty waypoints = patrolSO.FindProperty("waypoints");
        waypoints.arraySize = route.Count;
        for (int i = 0; i < route.Count; i++)
            waypoints.GetArrayElementAtIndex(i).objectReferenceValue = route[i];
        patrolSO.ApplyModifiedPropertiesWithoutUndo();

        KitchenRaceManager manager = new GameObject("Beach Race Manager").AddComponent<KitchenRaceManager>();
        manager.transform.SetParent(root.transform);
        SerializedObject managerSO = new SerializedObject(manager);
        managerSO.FindProperty("player").objectReferenceValue = player.transform;
        SerializedProperty managerRoute = managerSO.FindProperty("route");
        managerRoute.arraySize = route.Count;
        for (int i = 0; i < route.Count; i++) managerRoute.GetArrayElementAtIndex(i).objectReferenceValue = route[i];
        managerSO.FindProperty("targetLaps").intValue = 2;
        managerSO.ApplyModifiedPropertiesWithoutUndo();

        RaceStartCountdown countdown = new GameObject("RaceStartCountdown").AddComponent<RaceStartCountdown>();
        countdown.transform.SetParent(root.transform);
        AssignCountdownSprites(countdown);

        // PlayerRig already contains the configured follow camera. The original
        // showcase camera in Beach must be disabled or it competes for Display 1
        // and leaves two AudioListeners active.
        Camera playerCamera = player.GetComponentInChildren<Camera>(true);
        foreach (Camera sceneCamera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (sceneCamera != playerCamera) sceneCamera.gameObject.SetActive(false);
        if (playerCamera != null && playerCamera.GetComponent<CinemachineBrain>() == null)
            playerCamera.gameObject.AddComponent<CinemachineBrain>();
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            playerCamera.enabled = true;
            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
            orbitCamera.SnapToTarget();
        }

        Selection.activeGameObject = route[0].parent.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Beach race installed: player, three animal AI racers, two laps, ranking, countdown, pause UI, editable route and Beach NavMesh.");
    }

    [MenuItem("Tools/Adventure/Rebake Beach NavMesh Only")]
    public static void RebakeOnly()
    {
        Scene scene = EditorSceneManager.OpenScene(BeachScene, OpenSceneMode.Single);
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null) throw new System.InvalidOperationException("Install the Beach race first.");
        surface.BuildNavMesh();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Beach NavMesh rebaked. Orange BeachRoute points remain editable in Scene view.");
    }

    [MenuItem("Tools/Adventure/Refresh Beach Route Shortcuts And Loop")]
    public static void RefreshRouteLinks()
    {
        Scene scene = EditorSceneManager.OpenScene(BeachScene, OpenSceneMode.Single);
        GameObject route = GameObject.Find(RouteName);
        if (route == null) throw new System.InvalidOperationException("Beach route holder was not found.");
        InstallRouteLinksAndSave(scene, route.transform);
    }

    private static void InstallRouteLinksAndSave(Scene scene, Transform routeHolder)
    {
        var route = new List<Transform>();
        for (int i = 0; i < routeHolder.childCount; i++)
            if (routeHolder.GetChild(i).name.StartsWith("BeachRoute_"))
                route.Add(routeHolder.GetChild(i));
        NavMeshAgent sceneAgent = Object.FindFirstObjectByType<NavMeshAgent>();
        var filter = new NavMeshQueryFilter
        {
            agentTypeID = sceneAgent != null ? sceneAgent.agentTypeID : 0,
            areaMask = sceneAgent != null ? sceneAgent.areaMask : NavMesh.AllAreas
        };
        foreach (Transform point in route)
        {
            if (NavMesh.SamplePosition(point.position, out NavMeshHit hit, 6f, filter))
                point.position = hit.position;
            else
                Debug.LogWarning($"Could not snap {point.name} onto the Beach NavMesh.", point);
        }
        InstallRouteLinks(routeHolder.parent, route);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Beach shortcut links refreshed, including the last-to-first loop link.");
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
            if (start == null || end == null) continue;

            Vector3 startPosition = start.position;
            Vector3 endPosition = end.position;
            bool boundaryBetween = NavMesh.Raycast(startPosition, endPosition, out _, NavMesh.AllAreas);
            var path = new NavMeshPath();
            bool completePath = NavMesh.CalculatePath(startPosition, endPosition, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete;
            float directDistance = Vector3.Distance(startPosition, endPosition);
            float pathDistance = PathLength(path);
            bool finalLoop = i == route.Count - 1;
            bool wastefulDetour = completePath && pathDistance > directDistance * 1.35f + 0.5f;

            // Only create a jump where normal navigation is blocked/too indirect.
            // The final segment is always explicit so the patrol can start lap two.
            if (!finalLoop && !boundaryBetween && completePath && !wastefulDetour) continue;
            if (!finalLoop && directDistance > 12f) continue;

            GameObject linkObject = new GameObject(finalLoop
                ? "BeachLoop_Last_to_First"
                : $"BeachShortcut_{i + 1:00}_to_{i + 2:00}");
            linkObject.transform.SetParent(holder.transform, false);
            NavMeshLink link = linkObject.AddComponent<NavMeshLink>();
            link.startTransform = start;
            link.endTransform = end;
            link.width = 0.9f;
            link.bidirectional = false;
            link.costModifier = Mathf.Max(0.1f, directDistance * 0.8f);
            link.autoUpdate = true;
        }
    }

    private static float PathLength(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2)
            return float.PositiveInfinity;
        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return length;
    }

    private static Bounds FindBeachBounds()
    {
        GameObject floor = GameObject.Find("Beach_Floor");
        Transform searchRoot = floor != null ? floor.transform : null;

        Terrain terrain = searchRoot != null
            ? searchRoot.GetComponentInChildren<Terrain>(true)
            : Object.FindFirstObjectByType<Terrain>();
        if (terrain != null)
            return new Bounds(terrain.transform.position + terrain.terrainData.size * 0.5f, terrain.terrainData.size);

        Renderer[] renderers = searchRoot != null ? searchRoot.GetComponentsInChildren<Renderer>(true) : null;
        if (renderers == null || renderers.Length == 0)
            renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        Collider[] colliders = searchRoot != null ? searchRoot.GetComponentsInChildren<Collider>(true) : null;
        if (colliders == null || colliders.Length == 0)
            colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (colliders.Length == 0) throw new System.InvalidOperationException("No Beach floor, terrain, renderer or collider was found.");
        Bounds colliderBounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++) colliderBounds.Encapsulate(colliders[i].bounds);
        return colliderBounds;
    }

    private static List<Transform> CreateEditableRoute(Transform root, Bounds bounds)
    {
        GameObject holder = new GameObject(RouteName);
        holder.transform.SetParent(root);
        float x = Mathf.Max(3f, bounds.extents.x * 0.68f);
        float z = Mathf.Max(3f, bounds.extents.z * 0.68f);
        float y = bounds.max.y + 0.2f;
        Vector3 c = new Vector3(bounds.center.x, y, bounds.center.z);
        var result = new List<Transform>();
        const int pointCount = 16;
        for (int i = 0; i < pointCount; i++)
        {
            // Start at the south-west side and continue counter-clockwise.
            float angle = (-135f + i * 360f / pointCount) * Mathf.Deg2Rad;
            Vector3 desired = c + new Vector3(Mathf.Cos(angle) * x, 0f, Mathf.Sin(angle) * z);
            if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, Mathf.Max(x, z) * 0.45f, NavMesh.AllAreas))
                continue;
            GameObject point = new GameObject("BeachRoute_" + (result.Count + 1).ToString("00"));
            point.transform.SetParent(holder.transform);
            point.transform.position = hit.position;
            AIPatrolWaypoint marker = point.AddComponent<AIPatrolWaypoint>();
            SerializedObject markerSO = new SerializedObject(marker);
            markerSO.FindProperty("waitDuration").floatValue = 0f;
            markerSO.FindProperty("arrivalDistance").floatValue = 0.45f;
            markerSO.FindProperty("navMeshSampleDistance").floatValue = 1.25f;
            markerSO.ApplyModifiedPropertiesWithoutUndo();
            result.Add(point.transform);
        }
        return result;
    }

    private static Transform FindChildNamed(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == childName) return child;
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
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
                if (asset is Sprite sprite) { so.FindProperty(properties[i]).objectReferenceValue = sprite; break; }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
