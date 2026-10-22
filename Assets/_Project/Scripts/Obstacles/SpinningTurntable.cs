using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SpinningTurntable : MonoBehaviour
    {
        [Tooltip("Signed speed in degrees per second. Negative reverses rotation.")]
        [SerializeField] private float angularSpeed = 300f;
        [Tooltip("Maximum centripetal acceleration the player's grip can sustain (m/s²). Lower ejects more easily.")]
        [SerializeField, Min(0f)] private float gripAcceleration = 40f;
        [Tooltip("World-space radius around the axis where players are never automatically ejected.")]
        [SerializeField, Min(0f)] private float safeRadius = 0.75f;
        [Tooltip("Minimum continuous time on this turntable before automatic ejection is allowed.")]
        [SerializeField, Min(0f)] private float ejectionDelay = 0.35f;
        [Tooltip("Multiplier on tangential speed inherited by the launched player.")]
        [SerializeField, Min(0f)] private float tangentialMultiplier = 1f;
        [Tooltip("Additional horizontal speed away from the centre (m/s).")]
        [SerializeField, Min(0f)] private float outwardSpeed = 3f;
        [Tooltip("Upward launch speed (m/s). Makes the player clear the rim before falling under player gravity.")]
        [SerializeField, Min(0f)] private float upwardSpeed = 5f;
        private Rigidbody body;
        private Quaternion restLocalRotation;
        public float AngleDegrees { get; private set; }

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
            Quaternion parent = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            body.MoveRotation(parent * restLocalRotation * Quaternion.AngleAxis(AngleDegrees, Vector3.forward));
        }
        public Vector3 GetTangentialVelocity(Vector3 position)
        {
            Vector3 radius = Vector3.ProjectOnPlane(position - transform.position, transform.forward);
            return Vector3.Cross(transform.forward * (angularSpeed * Mathf.Deg2Rad), radius);
        }
        public bool TryGetLaunch(Vector3 position, float rideTime, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            if (!isActiveAndEnabled || rideTime < ejectionDelay) return false;
            Vector3 radius = Vector3.ProjectOnPlane(position - transform.position, transform.forward);
            float omega = angularSpeed * Mathf.Deg2Rad;
            if (radius.magnitude <= safeRadius || omega * omega * radius.magnitude <= gripAcceleration) return false;
            Vector3 outward = Vector3.ProjectOnPlane(radius, Vector3.up).normalized;
            velocity = Vector3.ProjectOnPlane(GetTangentialVelocity(position), Vector3.up) * tangentialMultiplier
                + outward * outwardSpeed + Vector3.up * upwardSpeed;
            return true;
        }
        private void OnValidate()
        {
            gripAcceleration = Mathf.Max(0f, gripAcceleration);
            safeRadius = Mathf.Max(0f, safeRadius);
            ejectionDelay = Mathf.Max(0f, ejectionDelay);
            tangentialMultiplier = Mathf.Max(0f, tangentialMultiplier);
            outwardSpeed = Mathf.Max(0f, outwardSpeed);
            upwardSpeed = Mathf.Max(0f, upwardSpeed);
        }
    }
}