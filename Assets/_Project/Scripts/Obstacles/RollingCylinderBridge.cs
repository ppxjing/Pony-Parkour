using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public sealed class RollingCylinderBridge : MonoBehaviour
    {
        [Tooltip("绕滚筒本地 Z 轴旋转，单位 度/秒；负数反转，0 停止。")]
        [SerializeField] private float angularSpeed = 360f;
        [Tooltip("抓地能维持的向心加速度（米/秒²）。转速² × 到轴距离超过此值时甩出；越小越容易甩出。")]
        [SerializeField, Min(0f)] private float gripAcceleration = 4f;
        [Tooltip("连续站在滚筒上多少秒后允许甩出。过长时可能先滑下滚筒。")]
        [SerializeField, Min(0f)] private float ejectionDelay = 0.08f;
        [Tooltip("甩出时继承切线速度的倍率。")]
        [SerializeField, Min(0f)] private float tangentialMultiplier = 2f;
        [Tooltip("额外朝远离滚筒轴的方向推开速度，米/秒。")]
        [SerializeField, Min(0f)] private float outwardSpeed = 1f;
        [Tooltip("额外向上抛出的速度，米/秒，之后使用玩家重力下落。")]
        [SerializeField, Min(0f)] private float upwardSpeed = 4f;

        private Rigidbody body;
        private Quaternion restLocalRotation;
        public float AngleDegrees { get; private set; }
        public Vector3 Axis => transform.forward;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            restLocalRotation = transform.localRotation;
        }

        private void FixedUpdate()
        {
            AngleDegrees = Mathf.Repeat(AngleDegrees + angularSpeed * Time.fixedDeltaTime, 360f);
            Quaternion parentRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            body.MoveRotation(parentRotation * restLocalRotation * Quaternion.AngleAxis(AngleDegrees, Vector3.forward));
        }

        public bool IsStandingSurface(Vector3 point, Vector3 normal)
        {
            Vector3 radial = Vector3.ProjectOnPlane(point - transform.position, Axis);
            // Only the curved upper surface supports riders; the flat end caps and sides cannot launch.
            return isActiveAndEnabled && radial.sqrMagnitude > 0.0001f
                && Vector3.Dot(normal, Vector3.up) > 0.5f
                && Vector3.Dot(normal, radial.normalized) > 0.7f
                && Mathf.Abs(Vector3.Dot(normal, Axis)) < 0.5f;
        }

        public Vector3 GetTangentialVelocity(Vector3 point)
        {
            Vector3 radial = Vector3.ProjectOnPlane(point - transform.position, Axis);
            return Vector3.Cross(Axis * (angularSpeed * Mathf.Deg2Rad), radial);
        }

        public bool TryGetLaunch(Vector3 point, float rideTime, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            if (!isActiveAndEnabled || rideTime < ejectionDelay) return false;
            Vector3 radial = Vector3.ProjectOnPlane(point - transform.position, Axis);
            float omega = angularSpeed * Mathf.Deg2Rad;
            if (radial.sqrMagnitude < 0.0001f || omega * omega * radial.magnitude <= gripAcceleration)
                return false;
            velocity = GetTangentialVelocity(point) * tangentialMultiplier
                + radial.normalized * outwardSpeed + Vector3.up * upwardSpeed;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Color previous = Gizmos.color;
            Gizmos.color = Color.yellow;
            Vector3 axisLength = Axis * Mathf.Abs(transform.lossyScale.z) * 1.25f;
            Gizmos.DrawLine(transform.position - axisLength, transform.position + axisLength);
            Gizmos.DrawWireSphere(transform.position, 0.07f);
            Gizmos.color = previous;
        }
    }
}
