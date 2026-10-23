using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public sealed class RotatingPillarRod : MonoBehaviour
    {
        public enum MotionMode
        {
            [InspectorName("连续旋转")] Continuous = 0,
            [InspectorName("指定角度往复")] PingPong = 1
        }

        [Tooltip("连续旋转：完整转圈。指定角度往复：从起始角度转到终点，再原路返回。")]
        [SerializeField] private MotionMode motionMode = MotionMode.Continuous;
        [Tooltip("转速，度/秒。0 为停止；上下棍分别设置。")]
        [SerializeField, Min(0f)] private float angularSpeed = 60f;
        [Tooltip("反转连续旋转方向，或往复运动的初始方向。")]
        [SerializeField] private bool reverseDirection;
        [Tooltip("相对于编辑态摆放姿态的起始偏角，度；进入 Play 后生效。")]
        [SerializeField] private float startAngle;
        [Tooltip("往复模式单程旋转角度，度。连续模式始终完整转圈，此参数不参与计算。")]
        [SerializeField, Range(0f, 360f)] private float rotationAngle = 90f;

        private Rigidbody body;
        private Quaternion restLocalRotation;
        private float phase;
        public float AngleDegrees { get; private set; }
        public Vector3 Axis => transform.parent != null
            ? transform.parent.rotation * restLocalRotation * Vector3.up
            : restLocalRotation * Vector3.up;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            restLocalRotation = transform.localRotation;
            AngleDegrees = startAngle;
            body.rotation = WorldRotation();
        }

        private void FixedUpdate()
        {
            float step = Mathf.Max(0f, angularSpeed) * Time.fixedDeltaTime;
            float offset;
            if (motionMode == MotionMode.PingPong)
            {
                float range = Mathf.Clamp(rotationAngle, 0f, 360f);
                if (range <= 0.0001f)
                {
                    phase = 0f;
                    offset = 0f;
                }
                else
                {
                    phase = Mathf.Repeat(phase + step, range * 2f);
                    offset = range - Mathf.Abs(phase - range);
                }
            }
            else
            {
                phase = Mathf.Repeat(phase + step, 360f);
                offset = phase;
            }
            AngleDegrees = startAngle + (reverseDirection ? -offset : offset);
            body.MoveRotation(WorldRotation());
        }

        private Quaternion WorldRotation()
        {
            Quaternion parent = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            return parent * restLocalRotation * Quaternion.AngleAxis(AngleDegrees, Vector3.up);
        }

        private void OnDrawGizmosSelected()
        {
            Color previous = Gizmos.color;
            Gizmos.color = Color.cyan;
            Vector3 axis = Application.IsPlaying(gameObject) ? Axis : transform.up;
            Gizmos.DrawLine(transform.position - axis * .5f, transform.position + axis * .5f);
            Gizmos.DrawWireSphere(transform.position, .06f);
            Gizmos.color = previous;
        }
    }
}
