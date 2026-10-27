using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InstallBeachDeathZones
{
    private const string BeachScene = "Assets/_Project/Scenes/Beach.unity";
    private const string DeathZonePrefab = "Assets/_prefab/死亡区域/PF_DeathZone.prefab";
    private const string RouteName = "Beach Race Route (MOVE THESE POINTS)";
    private const string RootName = "Beach Death Zones (MOVE AND SCALE)";

    [InitializeOnLoadMethod]
    private static void InstallWhenBeachIsOpen()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.name == "Beach" && GameObject.Find(RootName) == null)
                Install(scene);
        };
    }

    [MenuItem("Tools/Adventure/Install or Reset Beach Death Zones")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(BeachScene, OpenSceneMode.Single);
        Install(scene);
    }

    private static void Install(Scene scene)
    {
        GameObject routeHolder = GameObject.Find(RouteName);
        if (routeHolder == null)
            throw new System.InvalidOperationException("Install the Beach race route before creating death zones.");

        var points = new List<Transform>();
        for (int i = 0; i < routeHolder.transform.childCount; i++)
            if (routeHolder.transform.GetChild(i).name.StartsWith("BeachRoute_"))
                points.Add(routeHolder.transform.GetChild(i));
        if (points.Count == 0)
            throw new System.InvalidOperationException("The Beach route has no points.");

        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);
        GameObject root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        Bounds bounds = new Bounds(points[0].position, Vector3.zero);
        foreach (Transform point in points) bounds.Encapsulate(point.position);

        const float margin = 4f;
        const float borderThickness = 8f;
        float verticalBottom = bounds.min.y - 3f;
        float verticalTop = bounds.max.y + 5f;
        float borderHeight = Mathf.Max(8f, verticalTop - verticalBottom);
        float borderY = (verticalBottom + verticalTop) * 0.5f;
        float wideX = bounds.size.x + (margin + borderThickness) * 2f;
        float wideZ = bounds.size.z + (margin + borderThickness) * 2f;

        CreateZone(root.transform, "DeathZone_Below_Map",
            new Vector3(bounds.center.x, bounds.min.y - 4f, bounds.center.z),
            new Vector3(wideX, 5f, wideZ));
        CreateZone(root.transform, "DeathZone_Ocean_West",
            new Vector3(bounds.min.x - margin - borderThickness * 0.5f, borderY, bounds.center.z),
            new Vector3(borderThickness, borderHeight, wideZ));
        CreateZone(root.transform, "DeathZone_Ocean_East",
            new Vector3(bounds.max.x + margin + borderThickness * 0.5f, borderY, bounds.center.z),
            new Vector3(borderThickness, borderHeight, wideZ));
        CreateZone(root.transform, "DeathZone_Ocean_North",
            new Vector3(bounds.center.x, borderY, bounds.max.z + margin + borderThickness * 0.5f),
            new Vector3(wideX, borderHeight, borderThickness));
        CreateZone(root.transform, "DeathZone_Ocean_South",
            new Vector3(bounds.center.x, borderY, bounds.min.z - margin - borderThickness * 0.5f),
            new Vector3(wideX, borderHeight, borderThickness));

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Beach death zones installed: bottom fall zone plus four editable ocean boundary zones.");
    }

    private static void CreateZone(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(DeathZonePrefab);
        if (asset == null) throw new System.InvalidOperationException("PF_DeathZone prefab is missing.");
        GameObject zone = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.gameObject.scene);
        zone.name = name;
        zone.transform.SetParent(parent, true);
        zone.transform.SetPositionAndRotation(position, Quaternion.identity);
        zone.transform.localScale = size;
    }
}
