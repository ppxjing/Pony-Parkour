using UnityEngine;

namespace Adventure.AI
{
    [DisallowMultipleComponent]
    public sealed class AIPatrolWaypoint : MonoBehaviour
    {
        [Tooltip("AI 到达此点后的停留秒数；0 表示立即继续。")]
        [SerializeField, Min(0f)] private float waitDuration = 2f;
        [Tooltip("到达判定距离，单位米，不受 Transform Scale 影响。")]
        [SerializeField, Min(0.02f)] private float arrivalDistance = 0.15f;
        [Tooltip("允许从标记位置寻找 NavMesh 的最大距离。应贴近蓝色 NavMesh 放置，避免吸附到其他楼层。")]
        [SerializeField, Min(0.02f)] private float navMeshSampleDistance = 0.3f;

        public float WaitDuration => Mathf.Max(0f, waitDuration);
        public float ArrivalDistance => Mathf.Max(0.02f, arrivalDistance);
        public float NavMeshSampleDistance => Mathf.Max(0.02f, navMeshSampleDistance);

        private void OnDrawGizmos()
        {
            if (Application.IsPlaying(gameObject)) return;
            Color oldColor = Gizmos.color;
            Vector3 p = transform.position;
            Gizmos.color = new Color(1f, 0.75f, 0.05f, 0.9f);
            Gizmos.DrawSphere(p + Vector3.up * 0.55f, 0.09f);
            Gizmos.DrawLine(p, p + Vector3.up * 0.55f);
            Vector3 previous = p + Vector3.right * ArrivalDistance;
            for (int i = 1; i <= 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32f;
                Vector3 next = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ArrivalDistance;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
#if UNITY_EDITOR
            UnityEditor.Handles.Label(p + Vector3.up * 0.7f, name + "  停留 " + WaitDuration.ToString("0.##") + " 秒");
#endif
            Gizmos.color = oldColor;
        }
    }
}
