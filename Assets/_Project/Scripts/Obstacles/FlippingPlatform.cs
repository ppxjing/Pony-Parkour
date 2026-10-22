using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FlippingPlatform : MonoBehaviour
    {
        public enum RotationMode
        {
            [InspectorName("单方向旋转")] SingleDirection = 0,
            [InspectorName("来回交替")] Alternating = 1
        }

        [Tooltip("Axis in the pivot's initial local space. This model hinges around X.")]
        [SerializeField] private Vector3 localAxis = Vector3.right;
        [SerializeField, Min(0.05f)] private float flipDuration = 0.8f;
        [SerializeField, Min(0f)] private float pauseDuration = 2f;
        [SerializeField, Min(0f)] private float startDelay = 2f;
        [Tooltip("Higher values start faster and slow down more strongly. Must exceed 1.")]
        [SerializeField, Range(1.1f, 5f)] private float easeOutPower = 3f;
        [Tooltip("Reverse the initial direction. In Alternating mode, subsequent turns switch direction.")]
        [SerializeField] private bool reverseDirection = true;
        [Tooltip("Rotate in one direction, or alternate equal turns out and back.")]
        [SerializeField] private RotationMode motionMode = RotationMode.SingleDirection;
        [Tooltip("Positive angle in degrees for each turn; direction is controlled by Reverse Direction.")]
        [SerializeField, Min(0.1f)] private float flipAngle = 180f;
        [SerializeField] private float startAngle;
        private Rigidbody body;
        private Quaternion restLocalRotation;
        private float waitRemaining, elapsed, turnDuration, turnPower, fromAngle, turnDelta;
        public bool IsFlipping { get; private set; }
        public int CompletedFlips { get; private set; }
        public float AngleDegrees { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            restLocalRotation = transform.localRotation;
            AngleDegrees = startAngle;
            waitRemaining = Mathf.Max(0f, startDelay);
            body.rotation = WorldRotation();
        }
        private void FixedUpdate()
        {
            float remaining = Time.fixedDeltaTime;
            // Consume the full physics step, including a wait/turn boundary.
            while (remaining > 0f)
            {
                if (!IsFlipping)
                {
                    float wait = Mathf.Min(waitRemaining, remaining);
                    waitRemaining -= wait;
                    remaining -= wait;
                    if (remaining <= 0f) break;
                    BeginFlip();
                }
                float step = Mathf.Min(remaining, turnDuration - elapsed);
                elapsed += step;
                remaining -= step;
                float t = Mathf.Clamp01(elapsed / turnDuration);
                AngleDegrees = fromAngle + turnDelta * (1f - Mathf.Pow(1f - t, turnPower));
                if (elapsed >= turnDuration)
                {
                    AngleDegrees = Mathf.Repeat(fromAngle + turnDelta, 360f);
                    CompletedFlips++;
                    IsFlipping = false;
                    waitRemaining = Mathf.Max(0f, pauseDuration);
                }
            }
            body.MoveRotation(WorldRotation());
        }
        private void BeginFlip()
        {
            IsFlipping = true;
            elapsed = 0f;
            fromAngle = AngleDegrees;
            turnDuration = Mathf.Max(0.05f, flipDuration);
            turnPower = Mathf.Clamp(easeOutPower, 1.1f, 5f);
            float sign = reverseDirection ? -1f : 1f;
            if (motionMode == RotationMode.Alternating && CompletedFlips % 2 == 1) sign = -sign;
            turnDelta = Mathf.Max(0.1f, flipAngle) * sign;
        }
        private Quaternion WorldRotation()
        {
            Vector3 axis = localAxis.sqrMagnitude > 0.0001f ? localAxis.normalized : Vector3.right;
            Quaternion parent = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            return parent * restLocalRotation * Quaternion.AngleAxis(AngleDegrees, axis);
        }
        private void OnValidate()
        {
            if (localAxis.sqrMagnitude < 0.0001f) localAxis = Vector3.right;
            flipDuration = Mathf.Max(0.05f, flipDuration);
            flipAngle = Mathf.Max(0.1f, flipAngle);
            pauseDuration = Mathf.Max(0f, pauseDuration);
            startDelay = Mathf.Max(0f, startDelay);
            easeOutPower = Mathf.Clamp(easeOutPower, 1.1f, 5f);
        }
        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying) return;
            Vector3 axis = transform.TransformDirection(localAxis.normalized);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position - axis, transform.position + axis);
        }
    }
}