using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class InflatingBounceBall : MonoBehaviour
    {
        [Header("Visual and size")]
        [SerializeField] private Transform visual;
        [Tooltip("Normal ball radius in local units. Keep the root scale positive and uniform.")]
        [SerializeField, Min(0.01f)] private float baseRadius = 1.1f;
        [SerializeField, Min(1f)] private float inflatedScale = 1.5f;
        [Tooltip("Distance above the bounce plane over which the descending ball rapidly expands.")]
        [SerializeField, Min(0.01f)] private float inflationDistance = 0.6f;
        [SerializeField, Min(0f)] private float inflatedHoldTime = 0.08f;
        [SerializeField, Min(0.01f)] private float recoveryTime = 0.25f;

        [Header("Gravity and bounce")]
        [Tooltip("Rise of the ball CENTER above the bounce plane, in world metres.")]
        [SerializeField, Min(0.1f)] private float bounceHeight = 3f;
        [Tooltip("Downward acceleration in m/s². This is local to this mechanism.")]
        [SerializeField, Min(0.1f)] private float gravity = 9.81f;
        [Tooltip("Gap under the fully expanded ball at the instant of bounce.")]
        [SerializeField, Min(0f)] private float groundClearance = 0.03f;

        [Header("Stationary ground detection")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(1f)] private float groundSearchDistance = 50f;

        private Rigidbody body;
        private SphereCollider sphere;
        private Vector3 normalVisualScale, spawnPosition;
        private float height, velocity, timeSinceBounce;
        private bool initialized;
        private readonly RaycastHit[] groundHits = new RaycastHit[64];

        public int BounceCount { get; private set; }
        public float VerticalVelocity => velocity;
        public float BouncePlaneY { get; private set; }
        public float GroundY { get; private set; }
        public float CurrentScale { get; private set; } = 1f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            sphere = GetComponent<SphereCollider>();
            if (visual == null || visual == transform)
            {
                Debug.LogError("InflatingBounceBall requires a child Visual reference.", this);
                enabled = false;
                return;
            }
            normalVisualScale = visual.localScale;
            spawnPosition = transform.position;
            body.isKinematic = true;
            body.useGravity = false; // This component owns the exact ballistic trajectory.
            body.interpolation = RigidbodyInterpolation.Interpolate;
            initialized = true;
        }

        private void OnEnable()
        {
            if (initialized) RestartMotion();
        }

        [ContextMenu("Restart motion and detect ground")]
        public void RestartMotion()
        {
            if (!Application.isPlaying || !initialized) return;
            Vector3 scale = transform.lossyScale;
            if (scale.x <= 0f || Mathf.Abs(scale.x - scale.y) > 0.001f || Mathf.Abs(scale.x - scale.z) > 0.001f)
            {
                Debug.LogError("InflatingBounceBall needs a positive uniform root scale.", this);
                enabled = false;
                return;
            }
            Physics.SyncTransforms();
            float nearest = float.PositiveInfinity;
            int count = Physics.RaycastNonAlloc(spawnPosition, Vector3.down, groundHits,
                groundSearchDistance, groundMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];
                if (hit.transform.IsChildOf(transform) || hit.collider is CharacterController ||
                    hit.rigidbody != null || hit.normal.y < 0.99f || hit.distance >= nearest) continue;
                nearest = hit.distance;
                GroundY = hit.point.y;
            }
            if (float.IsInfinity(nearest))
            {
                Debug.LogError("InflatingBounceBall: no stationary horizontal floor below the spawn. Check Ground Mask and colliders.", this);
                enabled = false;
                return;
            }
            BouncePlaneY = GroundY + baseRadius * scale.x * inflatedScale + groundClearance;
            height = Mathf.Max(spawnPosition.y, BouncePlaneY);
            velocity = 0f;
            timeSinceBounce = float.PositiveInfinity;
            BounceCount = 0;
            body.position = new Vector3(spawnPosition.x, height, spawnPosition.z);
            ApplySize();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            // Solve the exact descending crossing inside this step, then integrate the remainder.
            // This avoids bounce-height drift when the floor is crossed between physics ticks.
            float toImpact = (velocity + Mathf.Sqrt(velocity * velocity +
                2f * gravity * Mathf.Max(0f, height - BouncePlaneY))) / gravity;
            if (toImpact <= dt)
            {
                float launchSpeed = Mathf.Sqrt(2f * gravity * bounceHeight);
                float period = 2f * launchSpeed / gravity;
                float remaining = Mathf.Max(0f, dt - toImpact);
                int wholeCycles = Mathf.FloorToInt(remaining / period);
                remaining -= wholeCycles * period;
                BounceCount += 1 + wholeCycles;
                timeSinceBounce = remaining;
                height = BouncePlaneY + launchSpeed * remaining - 0.5f * gravity * remaining * remaining;
                velocity = launchSpeed - gravity * remaining;
            }
            else
            {
                height += velocity * dt - 0.5f * gravity * dt * dt;
                velocity -= gravity * dt;
                timeSinceBounce += dt;
            }
            ApplySize();
            body.MovePosition(new Vector3(spawnPosition.x, height, spawnPosition.z));
        }

        private void ApplySize()
        {
            float expansion;
            if (velocity <= 0f)
                expansion = Mathf.SmoothStep(0f, 1f, 1f - (height - BouncePlaneY) / inflationDistance);
            else
                expansion = 1f - Mathf.SmoothStep(0f, 1f, (timeSinceBounce - inflatedHoldTime) / recoveryTime);
            CurrentScale = Mathf.Lerp(1f, inflatedScale, expansion);
            visual.localScale = normalVisualScale * CurrentScale;
            sphere.radius = baseRadius * CurrentScale;
        }

        private void OnDisable()
        {
            if (!initialized) return;
            visual.localScale = normalVisualScale;
            sphere.radius = baseRadius;
            CurrentScale = 1f;
        }

        private void OnValidate()
        {
            baseRadius = Mathf.Max(0.01f, baseRadius);
            inflatedScale = Mathf.Max(1f, inflatedScale);
            inflationDistance = Mathf.Max(0.01f, inflationDistance);
            inflatedHoldTime = Mathf.Max(0f, inflatedHoldTime);
            recoveryTime = Mathf.Max(0.01f, recoveryTime);
            bounceHeight = Mathf.Max(0.1f, bounceHeight);
            gravity = Mathf.Max(0.1f, gravity);
            groundClearance = Mathf.Max(0f, groundClearance);
            groundSearchDistance = Mathf.Max(1f, groundSearchDistance);
        }
    }
}
