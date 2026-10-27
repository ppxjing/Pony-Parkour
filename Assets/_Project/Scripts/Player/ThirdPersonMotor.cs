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
        private SpinningTurntable standingTurntable;
        private RollingCylinderBridge standingRoller;
        private float rollerRideTime;
        public RollingCylinderBridge StandingRoller => standingRoller;
        private Vector3 externalHorizontalVelocity;
        private float turntableRideTime, detachUntil;
        public SpinningTurntable StandingTurntable => standingTurntable;
        private Transform SupportTransform => standingPlatform != null ? standingPlatform.transform :
            standingTurntable != null ? standingTurntable.transform :
            standingRoller != null ? standingRoller.transform : null;
        private Vector3 supportLocalPoint, supportWorldPoint;
        public SeesawPlatform StandingPlatform => standingPlatform;
        private Vector3 horizontalVelocity, spawnPosition;
        
        private float obstacleRespawnCooldownUntil;
        public bool IsObstacleStunned => false;
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
            FollowSupport(dt);
            if (!isActiveAndEnabled) return;
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
                if (standingTurntable != null)
                    externalHorizontalVelocity += Vector3.ProjectOnPlane(standingTurntable.GetTangentialVelocity(FootPosition), Vector3.up);
                if (standingRoller != null)
                    externalHorizontalVelocity += Vector3.ProjectOnPlane(standingRoller.GetTangentialVelocity(FootPosition), Vector3.up);
                ClearSupport();
                verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
                jumpRequested = lastGrounded = float.NegativeInfinity;
            }
            verticalVelocity = Mathf.Max(verticalVelocity + gravity * dt, -terminalSpeed);
            externalHorizontalVelocity = Vector3.MoveTowards(externalHorizontalVelocity, Vector3.zero,
                (controller.isGrounded && verticalVelocity <= 0f ? 25f : 1f) * dt);
            var previousTurntable = standingTurntable;
            var previousRoller = standingRoller;
            standingPlatform = null;
            standingTurntable = null;
            standingRoller = null;
            var flags = controller.Move((horizontalVelocity + externalHorizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if (!isActiveAndEnabled) return;
            if (standingTurntable != previousTurntable) turntableRideTime = 0f;
            if (previousTurntable != null && standingTurntable == null && verticalVelocity <= 0f)
                externalHorizontalVelocity += Vector3.ProjectOnPlane(previousTurntable.GetTangentialVelocity(FootPosition), Vector3.up);
            if (standingRoller != previousRoller) rollerRideTime = 0f;
            if (previousRoller != null && standingRoller == null && verticalVelocity <= 0f)
                externalHorizontalVelocity += Vector3.ProjectOnPlane(previousRoller.GetTangentialVelocity(FootPosition), Vector3.up);
            Transform support = SupportTransform;
            if (support != null)
            {
                // Roll the contact at the feet around the cylinder, keeping the capsule upright.
                supportWorldPoint = standingRoller != null ? FootPosition : transform.position;
                supportLocalPoint = support.InverseTransformPoint(supportWorldPoint);
            }
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            if (transform.position.y < respawnBelowY)
            {
                if (respawn != null && respawn.isActiveAndEnabled) respawn.RequestFallRecovery();
                else Respawn();
            }
        }
        private void FollowSupport(float dt)
        {
            Transform support = SupportTransform;
            if (support == null) return;
            if (verticalVelocity > 0f || (standingPlatform != null && !standingPlatform.isActiveAndEnabled)
                || (standingTurntable != null && !standingTurntable.isActiveAndEnabled)
                || (standingRoller != null && !standingRoller.isActiveAndEnabled))
            { ClearSupport(); return; }
            Vector3 point = support.TransformPoint(supportLocalPoint);
            // Carry through the controller so walls/ceilings still block the player.
            controller.Move(point - supportWorldPoint);
            supportWorldPoint = point;
            if (standingTurntable != null)
            {
                turntableRideTime += dt;
                Vector3 launch;
                if (standingTurntable.TryGetLaunch(FootPosition, turntableRideTime, out launch))
                {
                    LaunchFromSupport(launch);
                }
            }
            if (standingRoller != null)
            {
                rollerRideTime += dt;
                Vector3 launch;
                if (standingRoller.TryGetLaunch(FootPosition, rollerRideTime, out launch))
                    LaunchFromSupport(launch);
            }
        }
        private void LaunchFromSupport(Vector3 launch)
        {
            ClearSupport();
            externalHorizontalVelocity = Vector3.ProjectOnPlane(launch, Vector3.up);
            verticalVelocity = launch.y;
            detachUntil = Time.time + 0.2f;
            lastGrounded = jumpRequested = float.NegativeInfinity;
        }
        private void ClearSupport()
        {
            standingPlatform = null;
            standingTurntable = null;
            turntableRideTime = 0f;
            standingRoller = null;
            rollerRideTime = 0f;
        }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            
            var rotatingRod = hit.collider.GetComponentInParent<RotatingPillarRod>();
            if (rotatingRod != null && rotatingRod.isActiveAndEnabled) RespawnFromObstacle();

            
if (!isActiveAndEnabled) return;
            if (verticalVelocity <= 0f && Time.time >= detachUntil && Vector3.Dot(hit.normal, Vector3.up) > 0.5f)
            {
                var platform = hit.collider.GetComponentInParent<SeesawPlatform>();
                if (platform != null && platform.isActiveAndEnabled && Vector3.Dot(hit.normal, platform.transform.up) > 0.7f)
                {
                    standingTurntable = null;
                    standingRoller = null;
                    standingPlatform = platform;
                    platform.RegisterSupport(this);
                }
                var turntable = hit.collider.GetComponentInParent<SpinningTurntable>();
                if (turntable != null && turntable.isActiveAndEnabled && Vector3.Dot(hit.normal, turntable.transform.forward) > 0.7f)
                {
                    standingPlatform = null;
                    standingRoller = null;
                    standingTurntable = turntable;
                }
                var roller = hit.collider.GetComponentInParent<RollingCylinderBridge>();
                if (roller != null && roller.IsStandingSurface(hit.point, hit.normal))
                {
                    standingPlatform = null;
                    standingTurntable = null;
                    standingRoller = roller;
                }
            }
            var trampoline = hit.collider.GetComponent<TrampolineSurface>();
            if (trampoline != null) trampoline.TryBounce(this, hit.normal);
        }
        public bool TryBounce(float height)
        {
            if (!isActiveAndEnabled || controller == null || !controller.enabled || verticalVelocity > 0f || height <= 0f)
                return false;
            ClearSupport();
            verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * height);
            // Landing consumes buffered jump/coyote time so Space cannot overwrite the launch.
            lastGrounded = jumpRequested = float.NegativeInfinity;
            return true;
        }
private void OnDisable()
        {
            ClearSupport();
            externalHorizontalVelocity = Vector3.zero;
            detachUntil = 0f;
            obstacleRespawnCooldownUntil = 0f;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            lastGrounded = jumpRequested = float.NegativeInfinity;
        }
        private void OnValidate()
        { gravity = -Mathf.Max(0.1f, Mathf.Abs(gravity)); }
        public void Respawn()
        {
            if (respawn != null && respawn.isActiveAndEnabled && respawn.CurrentCheckpoint != null)
            {
                respawn.RespawnNow();
                return;
            }

            KitchenRaceManager race = FindFirstObjectByType<KitchenRaceManager>();
            if (race != null && race.RespawnPlayerAtLastCheckpoint(this)) return;
            if (respawn != null && respawn.isActiveAndEnabled) { respawn.RespawnNow(); return; }
            TeleportAndReset(spawnPosition, transform.rotation);
        }

        public void RespawnFromObstacle()
        {
            if (!isActiveAndEnabled || Time.time < obstacleRespawnCooldownUntil) return;
            obstacleRespawnCooldownUntil = Time.time + 0.35f;
            Respawn();
        }
        public void TeleportAndReset(Vector3 position, Quaternion rotation)
        {
            ClearSupport();
            externalHorizontalVelocity = Vector3.zero;
            detachUntil = 0f;
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = wasEnabled;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            lastGrounded = jumpRequested = float.NegativeInfinity;
        }
    

public void ApplyObstacleStun(float duration = 1.25f)
        {
            RespawnFromObstacle();
        }
}
}
