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
        [Header("Collision")]
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.2f;
        [SerializeField, Min(0)] private float collisionPadding = 0.08f;
        [SerializeField] private LayerMask obstructionMask = ~0;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private Vector3 pivot;

        private void OnEnable()
        { if (target != null) { pivot = target.position; SnapToTarget(); } }
        private void LateUpdate()
        {
            if (target == null || input == null) return;
            if (input.OrbitHeld)
            {
                // Mouse delta is already displacement per frame; do not multiply it by deltaTime.
                Vector2 delta = input.Look * mouseSensitivity;
                yaw += delta.x;
                pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
            }
            pivot = Vector3.Lerp(pivot, target.position, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            PlaceCamera();
        }
        public void SnapToTarget()
        { if (target == null) return; pivot = target.position; PlaceCamera(); }
        private void PlaceCamera()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 backward = rotation * Vector3.back;
            float safeDistance = distance;
            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, backward, hits, distance, obstructionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (playerRoot != null && hits[i].transform.IsChildOf(playerRoot)) continue;
                safeDistance = Mathf.Min(safeDistance, Mathf.Max(0f, hits[i].distance - collisionPadding));
            }
            transform.SetPositionAndRotation(pivot + backward * safeDistance, rotation);
        }
        private void OnValidate()
        { maxPitch = Mathf.Max(minPitch, maxPitch); pitch = Mathf.Clamp(pitch, minPitch, maxPitch); }
    }
}
