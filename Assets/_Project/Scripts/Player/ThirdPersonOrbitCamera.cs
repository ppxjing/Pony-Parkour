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
        private RaycastHit[] hits = new RaycastHit[32];
        private Vector3 pivot;
        private Quaternion smoothedRotation;
        private float currentDistance;
        private bool ownsCursor;
        private bool inputBlocked;
        private Transform livingTarget;
        private float livingPitch;
        private RaceStartCountdown startCountdown;
        private bool wasCountingDown;

        private void Awake()
        {
            // Beach originally contains a showcase camera. Always make the
            // PlayerRig camera authoritative when this prefab is spawned there.
            if (gameObject.scene.name == "Beach" || gameObject.scene.name == "Bedroom")
            {
                Camera ownCamera = GetComponent<Camera>();
                ownCamera.enabled = true;
                foreach (Camera sceneCamera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (sceneCamera != ownCamera) sceneCamera.gameObject.SetActive(false);

                Transform rig = transform.root;
                ThirdPersonMotor motor = rig.GetComponentInChildren<ThirdPersonMotor>(true);
                PlayerInputReader reader = rig.GetComponentInChildren<PlayerInputReader>(true);
                Transform foundTarget = null;
                foreach (Transform child in rig.GetComponentsInChildren<Transform>(true))
                    if (child.name == "CameraTarget") { foundTarget = child; break; }
                if (foundTarget != null) target = foundTarget;
                if (motor != null) playerRoot = motor.transform;
                if (reader != null) input = reader;
            }
            livingTarget = target;
            livingPitch = pitch;
        }

        private void OnEnable()
        {
            SnapToTarget();
            if (Application.isPlaying && lockCursor && !inputBlocked) CaptureCursor();
        }
        private void Start()
        {
            startCountdown = FindFirstObjectByType<RaceStartCountdown>();
            // Scene setup may place or rotate the player after this camera's OnEnable.
            AlignBehindPlayer();
        }

        private void AlignBehindPlayer()
        {
            if (playerRoot != null) yaw = playerRoot.eulerAngles.y;
            pitch = Mathf.Clamp(livingPitch, minPitch, maxPitch);
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null || input == null) return;
            if (GamePauseMenu.IsAnyPaused) return;
            bool countingDown = startCountdown != null && startCountdown.IsCountingDown;
            if (countingDown || wasCountingDown)
            {
                // Countdown runs at timeScale 0: snap instead of interpolating, and
                // discard mouse movement until the race starts (including the release frame).
                AlignBehindPlayer();
                wasCountingDown = countingDown;
                return;
            }
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
        public void Configure(Transform newTarget, Transform newPlayerRoot, PlayerInputReader newInput)
        {
            target = newTarget;
            livingTarget = newTarget;
            playerRoot = newPlayerRoot;
            input = newInput;
            SnapToTarget();
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
        public void RestorePlayerControl()
        {
            target = livingTarget;
            inputBlocked = false;
            yaw = playerRoot != null ? playerRoot.eulerAngles.y : yaw;
            pitch = Mathf.Clamp(livingPitch, minPitch, maxPitch);
            SnapToTarget();
            if (lockCursor) CaptureCursor();
        }
        private void PlaceCamera(bool snap)
        {
            Vector3 backward = smoothedRotation * Vector3.back;
            float safeDistance = distance;
            int count;
            // Ignored mechanisms still occupy query results. Grow the reusable buffer
            // if full so a wall behind a dense mechanism is not accidentally missed.
            while (true)
            {
                count = Physics.SphereCastNonAlloc(pivot, collisionRadius, backward, hits, distance, obstructionMask, QueryTriggerInteraction.Ignore);
                if (count < hits.Length) break;
                System.Array.Resize(ref hits, hits.Length * 2);
            }
            for (int i = 0; i < count; i++)
            {
                if (playerRoot != null && hits[i].collider.transform.IsChildOf(playerRoot)) continue;
                var ignored = hits[i].collider.GetComponentInParent<CameraObstructionIgnore>();
                if (ignored != null && ignored.isActiveAndEnabled) continue;
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
