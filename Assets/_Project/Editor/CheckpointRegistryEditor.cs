using Adventure.Checkpoints;
using UnityEditor;

[CustomEditor(typeof(CheckpointRegistry))]
public sealed class CheckpointRegistryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("将场景里的检查点实例拖入 Checkpoints 列表。生效顺序取决于玩家进入先后；没有登记的点不会记录。", MessageType.Info);
        var registry = (CheckpointRegistry)target;
        if (!EditorApplication.isPlaying || registry.Player == null) return;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("最近进入的检查点", registry.Player.CurrentCheckpoint, typeof(RespawnCheckpoint), true);
            EditorGUILayout.Vector3Field("已记录的复活位置", registry.Player.SavedPosition);
            EditorGUILayout.IntField("复活次数", registry.Player.RespawnCount);
        }
    }
    public override bool RequiresConstantRepaint() { return EditorApplication.isPlaying; }
}
