using Adventure.AI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AIPatrolWaypoint)), CanEditMultipleObjects]
public sealed class AIPatrolWaypointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("将此场景实例拖入 AI 的 Nav Mesh Loop Patrol → Waypoints 列表，并调整列表顺序。仅放进场景不会自动加入所有 AI。黄色圆环表示到达距离；Transform Position 是目的地，旋转和缩放不影响判定。", MessageType.Info);
    }
}

[CustomEditor(typeof(NavMeshLoopPatrol))]
public sealed class NavMeshLoopPatrolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Waypoints 按从上到下依次必经，可混用 Link End 与途经点。途经点使用自己的 Wait Duration；普通 Transform 使用 Waypoint Pause。无法到达时重试，不会跳过。", MessageType.Info);
        if (!EditorApplication.isPlaying) return;
        var patrol = (NavMeshLoopPatrol)target;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("当前目标索引（从 0 开始）", patrol.CurrentWaypointIndex);
            EditorGUILayout.Toggle("正在停留", patrol.IsWaiting);
            EditorGUILayout.FloatField("剩余停留秒数", patrol.RemainingWaitTime);
            EditorGUILayout.IntField("完成圈数", patrol.CompletedLoops);
            EditorGUILayout.Toggle("已结束", patrol.IsFinished);
        }
    }

    public override bool RequiresConstantRepaint() { return EditorApplication.isPlaying; }
}
