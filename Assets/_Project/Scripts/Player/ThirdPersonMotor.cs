using Adventure.Obstacles;
using UnityEngine;

namespace Adventure.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    [DisallowMultipleComponent]
    public sealed class ThirdPersonMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform cameraTransform;
        [Tooltip("Rotate this visual container, leaving the player collision root independent of the model.")]
        [SerializeField] private Transform visuals;
        [Header("Movement")]
        [SerializeField, Min(0)] private float moveSpeed = 5f;
        [SerializeField, Min(0.1f)] private float acceleration = 25f;
        [SerializeField, Min(0)] private float turnSpeed = 720f;
        [Header("Jump and gravity")]
        [SerializeField, Min(0)] private float jumpHeight = 1.4f;
        [SerializeField] private float gravity = -25f;
        [SerializeField, Min(1)] private float terminalSpeed = 35f;
        [SerializeField, Min(0)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0)] private float jumpBufferTime = 0.12f;
        [Header("Prototype recovery")]
        [SerializeField] private float respawnBelowY = -15f;
        private CharacterController controller;
        private PlayerRespawn respawn;
        private SeesawPlatform standingPlatform;
        private Vector3 supportLocalPoint, supportWorldPoint;
        public SeesawPlatform StandingPlatform => standingPlatform;
        private Vector3 horizontalVelocity, spawnPosition;
        private float verticalVelocity, lastGrounded = float.NegativeInfinity, jumpRequested = float.NegativeInfinity;
        public bool IsGrounded => controller != null && controller.isGrounded;
        public Vector3 FootPosition => controller.bounds.center - Vector3.up * controller.bounds.extents.y;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            respawn = GetComponent<PlayerRespawn>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            spawnPosition = transform.position;
            if (cameraTransform == null || visuals == null)
            { Debug.LogError("ThirdPersonMotor needs Camera Transform and Visuals references.", this); enabled = false; }
        }
        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            FollowSupport();
            Vector2 stick = Vector2.ClampMagnitude(input.Move, 1f);
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 desired = (forward * stick.y + right * stick.x) * moveSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, acceleration * dt);
            if (desired.sqrMagnitude > 0.001f)
                visuals.rotation = Quaternion.RotateTowards(visuals.rotation, Quaternion.LookRotation(desired), turnSpeed * dt);

            if (controller.isGrounded && verticalVelocity <= 0f)
            { lastGrounded = Time.time; verticalVelocity = -2f; }
            if (input.JumpPressed) jumpRequested = Time.time;
            if (Time.time - jumpRequested <= jumpBufferTime && Time.time - lastGrounded <= coyoteTime)
            {
                verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
                jumpRequested = lastGrounded = float.NegativeInfinity;
            }
            verticalVelocity = Mathf.Max(verticalVelocity + gravity * dt, -terminalSpeed);
            standingPlatform = null;
            var flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if (standingPlatform != null)
            {
                supportLocalPoint = standingPlatform.transform.InverseTransformPoint(transform.position);
                supportWorldPoint = transform.position;
            }
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            if (transform.position.y < respawnBelowY)
            {
                if (respawn != null && respawn.isActiveAndEnabled) respawn.RequestFallRecovery();
                else Respawn();
            }
        }
        private void FollowSupport()
        {
            if (standingPlatform == null) return;
            if (!standingPlatform.isActiveAndEnabled || verticalVelocity > 0f)
            { standingPlatform = null; return; }
            Vector3 point = standingPlatform.transform.TransformPoint(supportLocalPoint);
            // Carry through the controller so walls/ceilings still block the player.
            controller.Move(point - supportWorldPoint);
            supportWorldPoint = point;
        }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!isActiveAndEnabled) return;
            if (verticalVelocity <= 0f && Vector3.Dot(hit.normal, Vector3.up) > 0.5f)
            {
                var platform = hit.collider.GetComponentInParent<SeesawPlatform>();
                if (platform != null && platform.isActiveAndEnabled && Vector3.Dot(hit.normal, platform.transform.up) > 0.7f)
                {
                    standingPlatform = platform;
                    platform.RegisterSupport(this);
                }
            }
            var trampoline = hit.collider.GetComponent<TrampolineSurface>();
            if (trampoline != null) trampoline.TryBounce(this, hit.normal);
        }
        public bool TryBounce(float height)
        {
            if (!isActiveAndEnabled || controller == null || !controller.enabled || verticalVelocity > 0f || height <= 0f)
                return false;
            standingPlatform = null;
            verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * height);
            // Landing consumes buffered jump/coyote time so Space cannot overwrite the launch.
            lastGrounded = jumpRequested = float.NegativeInfinity;
            return true;
        }
        private void OnDisable()
        { standingPlatform = null; horizontalVelocity = Vector3.zero; verticalVelocity = 0f; lastGrounded = jumpRequested = float.NegativeInfinity; }
        private void OnValidate()
        { gravity = -Mathf.Max(0.1f, Mathf.Abs(gravity)); }
        public void Respawn()
        {
            if (respawn != null && respawn.isActiveAndEnabled) { respawn.RespawnNow(); return; }
            TeleportAndReset(spawnPosition, transform.rotation);
        }
        public void TeleportAndReset(Vector3 position, Quaternion rotation)
        {
            standingPlatform = null;
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = wasEnabled;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            lastGrounded = jumpRequested = float.NegativeInfinity;
        }
    }
}
