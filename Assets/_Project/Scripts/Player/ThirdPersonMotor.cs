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
        private Vector3 horizontalVelocity, spawnPosition;
        private float verticalVelocity, lastGrounded = float.NegativeInfinity, jumpRequested = float.NegativeInfinity;
        public bool IsGrounded => controller != null && controller.isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            spawnPosition = transform.position;
            if (cameraTransform == null || visuals == null)
            { Debug.LogError("ThirdPersonMotor needs Camera Transform and Visuals references.", this); enabled = false; }
        }
        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
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
            var flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            if (transform.position.y < respawnBelowY) Respawn();
        }
        private void OnDisable()
        { horizontalVelocity = Vector3.zero; verticalVelocity = 0f; lastGrounded = jumpRequested = float.NegativeInfinity; }
        private void OnValidate()
        { gravity = -Mathf.Max(0.1f, Mathf.Abs(gravity)); }
        public void Respawn()
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            lastGrounded = jumpRequested = float.NegativeInfinity;
        }
    }
}
