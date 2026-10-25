using System.Collections.Generic;
using Adventure.AI;
using Adventure.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class InstallKitchenRace
{
    private const string ScenePath = "Assets/_Project/Scenes/Kitchen_Adventure.unity";

    [MenuItem("Tools/Adventure/Install Four Runner Race")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ThirdPersonMotor playerMotor = Object.FindFirstObjectByType<ThirdPersonMotor>();
        NavMeshLoopPatrol[] patrols = Object.FindObjectsByType<NavMeshLoopPatrol>(FindObjectsSortMode.None);
        if (playerMotor == null || patrols.Length == 0) throw new System.InvalidOperationException("Player or source AI not found.");

        NavMeshLoopPatrol source = patrols[0];
        source.gameObject.name = "AI Red";
        var racers = new List<NavMeshLoopPatrol> { source };
        string[] names = { "AI Blue", "AI Green" };
        float[] speedFactors = { 0.94f, 1.06f };
        Vector3[] offsets = { source.transform.right * 1.1f, -source.transform.right * 1.1f };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject existing = GameObject.Find(names[i]);
            NavMeshLoopPatrol patrol;
            if (existing == null)
            {
                GameObject clone = Object.Instantiate(source.gameObject);
                clone.name = names[i];
                SceneManager.MoveGameObjectToScene(clone, scene);
                clone.transform.position = FindNavPosition(source.transform.position + offsets[i], source.GetComponent<NavMeshAgent>());
                patrol = clone.GetComponent<NavMeshLoopPatrol>();
            }
            else patrol = existing.GetComponent<NavMeshLoopPatrol>();
            NavMeshAgent agent = patrol.GetComponent<NavMeshAgent>();
            agent.speed = source.GetComponent<NavMeshAgent>().speed * speedFactors[i];
            racers.Add(patrol);
        }

        SerializedObject patrolSO = new SerializedObject(source);
        SerializedProperty waypointProp = patrolSO.FindProperty("waypoints");
        var route = new List<Transform>();
        for (int i = 0; i < waypointProp.arraySize; i++)
            route.Add(waypointProp.GetArrayElementAtIndex(i).objectReferenceValue as Transform);

        KitchenRaceManager manager = Object.FindFirstObjectByType<KitchenRaceManager>();
        if (manager == null) manager = new GameObject("Kitchen Race Manager").AddComponent<KitchenRaceManager>();
        SerializedObject managerSO = new SerializedObject(manager);
        managerSO.FindProperty("player").objectReferenceValue = playerMotor.transform;
        SetList(managerSO.FindProperty("opponents"), racers);
        SetList(managerSO.FindProperty("route"), route);
        managerSO.FindProperty("targetLaps").intValue = 2;
        managerSO.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Installed four-runner, two-lap kitchen race with final ranking.");
    }

    private static Vector3 FindNavPosition(Vector3 desired, NavMeshAgent template)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = template.agentTypeID, areaMask = template.areaMask };
        return NavMesh.SamplePosition(desired, out NavMeshHit hit, 3f, filter) ? hit.position + Vector3.up * template.baseOffset : desired;
    }

    private static void SetList<T>(SerializedProperty property, List<T> values) where T : Object
    {
        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
