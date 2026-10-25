using System;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Adventure.AI;

public static class RepairLink9To12Navigation
{
    private const string ScenePath = "Assets/_Project/Scenes/Kitchen_Adventure.unity";
    private const string SurfaceName = "Navigation_Link9_12";
    private const string DataPath = "Assets/_Project/Scenes/Kitchen_Adventure/NavMesh-Navigation_Link9_12.asset";

    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        NavMeshLink[] links = UnityEngine.Object.FindObjectsByType<NavMeshLink>(FindObjectsSortMode.None)
            .Where(l => l.gameObject.scene == scene && IsTargetLink(l.name))
            .OrderBy(l => l.name)
            .ToArray();

        if (links.Length != 3)
            throw new InvalidOperationException($"Expected 3 links for points 9-12, found {links.Length}.");

        GameObject existing = GameObject.Find(SurfaceName);
        if (existing != null)
        {
            NavMeshSurface oldSurface = existing.GetComponent<NavMeshSurface>();
            if (oldSurface != null) oldSurface.RemoveData();
            UnityEngine.Object.DestroyImmediate(existing);
        }

        Bounds routeBounds = new Bounds(GetStart(links[0]), Vector3.zero);
        foreach (NavMeshLink link in links)
        {
            routeBounds.Encapsulate(GetStart(link));
            routeBounds.Encapsulate(GetEnd(link));
        }
        routeBounds.Expand(new Vector3(5f, 6f, 5f));
        routeBounds.size = new Vector3(Mathf.Max(routeBounds.size.x, 8f), Mathf.Max(routeBounds.size.y, 6f), Mathf.Max(routeBounds.size.z, 8f));

        GameObject surfaceObject = new GameObject(SurfaceName);
        surfaceObject.transform.position = routeBounds.center;
        NavMeshSurface surface = surfaceObject.AddComponent<NavMeshSurface>();
        surface.agentTypeID = links[0].agentTypeID;
        surface.collectObjects = CollectObjects.Volume;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.center = Vector3.zero;
        surface.size = routeBounds.size;
        surface.overrideTileSize = true;
        surface.tileSize = 128;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.08f;
        surface.minRegionArea = 0.25f;
        surface.BuildNavMesh();

        if (surface.navMeshData == null)
            throw new InvalidOperationException("Navigation_Link9_12 bake did not create NavMesh data.");
        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(surface.navMeshData)))
        {
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(DataPath) != null)
                AssetDatabase.DeleteAsset(DataPath);
            AssetDatabase.CreateAsset(surface.navMeshData, DataPath);
        }

        RepairPatrolWaypoints(scene);

        int validEndpoints = 0;
        foreach (NavMeshLink link in links)
        {
            bool startValid = NavMesh.SamplePosition(GetStart(link), out NavMeshHit startHit, 1.5f, NavMesh.AllAreas);
            bool endValid = NavMesh.SamplePosition(GetEnd(link), out NavMeshHit endHit, 1.5f, NavMesh.AllAreas);
            if (startValid) validEndpoints++;
            if (endValid) validEndpoints++;
            Debug.Log($"{link.name.Trim()}: start NavMesh={startValid} ({GetStart(link)}), end NavMesh={endValid} ({GetEnd(link)})");
        }

        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"Link 9-12 navigation repaired. Surface bounds={routeBounds}, valid endpoints={validEndpoints}/6, data={AssetDatabase.GetAssetPath(surface.navMeshData)}");
    }

    private static void RepairPatrolWaypoints(Scene scene)
    {
        NavMeshLoopPatrol patrol = UnityEngine.Object.FindObjectsByType<NavMeshLoopPatrol>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.gameObject.scene == scene && p.name.Contains("NavigationTestAI"));
        if (patrol == null)
            throw new InvalidOperationException("NavigationTestAI patrol component was not found.");

        NavMeshLink[] routeLinks = UnityEngine.Object.FindObjectsByType<NavMeshLink>(FindObjectsSortMode.None)
            .Where(l => l.gameObject.scene == scene)
            .OrderBy(LinkNumber)
            .ToArray();
        SerializedObject serializedPatrol = new SerializedObject(patrol);
        SerializedProperty waypoints = serializedPatrol.FindProperty("waypoints");

        foreach (NavMeshLink link in routeLinks)
        {
            int number = LinkNumber(link);
            if (number < 9 || number > 12 || link.endTransform == null) continue;
            bool alreadyPresent = false;
            for (int i = 0; i < waypoints.arraySize; i++)
                if (waypoints.GetArrayElementAtIndex(i).objectReferenceValue == link.endTransform)
                    alreadyPresent = true;
            if (alreadyPresent) continue;
            int index = waypoints.arraySize;
            waypoints.InsertArrayElementAtIndex(index);
            waypoints.GetArrayElementAtIndex(index).objectReferenceValue = link.endTransform;
            Debug.Log($"Added patrol waypoint {index}: {link.name.Trim()} / {link.endTransform.position}");
        }

        serializedPatrol.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(patrol);
        EditorUtility.SetDirty(patrol);
        if (waypoints.arraySize != 13)
            Debug.LogWarning($"NavigationTestAI now has {waypoints.arraySize} waypoints; expected 13 (start plus 12 link ends).", patrol);
        else
            Debug.Log("NavigationTestAI patrol route now contains all 13 ordered waypoints (start plus Link 1-12 ends).", patrol);
    }

    private static int LinkNumber(NavMeshLink link)
    {
        string name = link.name.Trim();
        int start = name.IndexOf("Link_", StringComparison.OrdinalIgnoreCase);
        if (start < 0) return int.MaxValue;
        start += 5;
        int end = name.IndexOf("_to_", start, StringComparison.OrdinalIgnoreCase);
        if (end < 0 || !int.TryParse(name.Substring(start, end - start), out int number))
            return int.MaxValue;
        return number;
    }

    private static bool IsTargetLink(string name)
    {
        string n = name.Trim();
        return n.StartsWith("Link_9_to_10", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("Link_10_to_11", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("Link_11_to_12", StringComparison.OrdinalIgnoreCase);
    }

    private static Vector3 GetStart(NavMeshLink link) => link.startTransform != null
        ? link.startTransform.position
        : link.transform.TransformPoint(link.startPoint);

    private static Vector3 GetEnd(NavMeshLink link) => link.endTransform != null
        ? link.endTransform.position
        : link.transform.TransformPoint(link.endPoint);
}
