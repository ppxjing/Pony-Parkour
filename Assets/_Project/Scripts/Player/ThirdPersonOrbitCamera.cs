using UnityEngine;

namespace Adventure.Player
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class ThirdPersonOrbitCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private PlayerInputReader input;
        [Header("Framing")]
        [SerializeField, Min(0.5f)] private float distance = 4.5f;
        [SerializeField] private float yaw = 0f;
        [SerializeField] private float pitch = 18f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField, Min(0)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0.1f)] private float followSharpness = 16f;
        [SerializeField, Min(0.1f)] private float rotationSharpness = 12f;
        [SerializeField, Min(0.1f)] private float distanceSharpness = 10f;
        [Header("Mouse capture")]
        [SerializeField] private bool lockCursor = true;
        [Header("Collision")]
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.2f;
        [SerializeField, Min(0)] private float collisionPadding = 0.08f;
        [SerializeField] private LayerMask obstructionMask = ~0;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private Vector3 pivot;
        private Quaternion smoothedRotation;
        private float currentDistance;
        private bool ownsCursor;
        private bool inputBlocked;

        private void OnEnable()
        {
            SnapToTarget();
            if (Application.isPlaying && lockCursor && !inputBlocked) CaptureCursor();
        }
        private void LateUpdate()
        {
            if (target == null || input == null) return;
            bool capturedThisFrame = false;
            if (lockCursor && !inputBlocked)
            {
                if (input.ReleaseCursorPressed) ReleaseCursor();
                else if (input.CaptureCursorPressed && Cursor.lockState != CursorLockMode.Locked)
                { CaptureCursor(); capturedThisFrame = true; }
            }
            if (!inputBlocked && (!lockCursor || Cursor.lockState == CursorLockMode.Locked) && !capturedThisFrame)
            {
                // Mouse delta is already displacement per frame; do not multiply it by deltaTime.
                Vector2 delta = input.Look * mouseSensitivity;
                yaw += delta.x;
                pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
            }
            pivot = Vector3.Lerp(pivot, target.position, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            smoothedRotation = Quaternion.Slerp(smoothedRotation, Quaternion.Euler(pitch, yaw, 0), 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
            PlaceCamera(false);
        }
        public void SnapToTarget()
        {
            if (target == null) return;
            pivot = target.position;
            smoothedRotation = Quaternion.Euler(pitch, yaw, 0);
            currentDistance = distance;
            PlaceCamera(true);
        }
        public void FollowWithoutInput(Transform newTarget)
        {
            target = newTarget;
            inputBlocked = true;
            // Keep the current view direction while smoothly following the falling body.
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;
            ReleaseCursor();
        }
        private void PlaceCamera(bool snap)
        {
            Vector3 backward = smoothedRotation * Vector3.back;
            float safeDistance = distance;
            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, backward, hits, distance, obstructionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (playerRoot != null && hits[i].transform.IsChildOf(playerRoot)) continue;
                safeDistance = Mathf.Min(safeDistance, Mathf.Max(0f, hits[i].distance - collisionPadding));
            }
            // Move inward immediately to avoid clipping; ease outward when an obstruction clears.
            currentDistance = snap || safeDistance < currentDistance ? safeDistance : Mathf.Lerp(currentDistance, safeDistance, 1f - Mathf.Exp(-distanceSharpness * Time.deltaTime));
            transform.SetPositionAndRotation(pivot + backward * currentDistance, smoothedRotation);
        }
        private void CaptureCursor()
        { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; ownsCursor = true; }
        private void ReleaseCursor()
        { if (!ownsCursor) return; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; ownsCursor = false; }
        private void OnDisable() { ReleaseCursor(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseCursor(); }
        private void OnValidate()
        { maxPitch = Mathf.Max(minPitch, maxPitch); pitch = Mathf.Clamp(pitch, minPitch, maxPitch); }
    }
}
