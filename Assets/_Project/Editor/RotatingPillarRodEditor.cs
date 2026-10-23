using Adventure.Obstacles;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RotatingPillarRod)), CanEditMultipleObjects]
public sealed class RotatingPillarRodEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("motionMode"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("angularSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("reverseDirection"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startAngle"));
        var mode = serializedObject.FindProperty("motionMode");
        if (mode.hasMultipleDifferentValues || mode.enumValueIndex == 1)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationAngle"));
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.HelpBox("上棍选 UpperRotation，下棍选 LowerRotation，参数互不影响。轴心沿黄色立柱，围绕各自本地 Y 轴旋转。Start Angle 是编辑态摆放基础上的偏角，Play 时生效。", MessageType.Info);
        if (!EditorApplication.isPlaying || targets.Length != 1) return;
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.FloatField("当前偏角", ((RotatingPillarRod)target).AngleDegrees);
    }
    public override bool RequiresConstantRepaint() { return EditorApplication.isPlaying; }
}
