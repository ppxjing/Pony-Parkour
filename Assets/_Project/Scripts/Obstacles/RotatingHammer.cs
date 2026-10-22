using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RotatingHammer : MonoBehaviour
    {
        [Tooltip("Rotation axis in the pivot's initial local space. This model uses Z.")]
        [SerializeField] private Vector3 localAxis = Vector3.forward;
        [Tooltip("Degrees per second. Negative reverses direction; zero stops rotation.")]
        [SerializeField] private float degreesPerSecond = 90f;
        [SerializeField] private float startAngle;
        [SerializeField, Min(0f)] private float startDelay;
        private Rigidbody body;
        private Quaternion restLocalRotation;
        private float angle;
        private float remainingDelay;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            restLocalRotation = transform.localRotation;
            angle = startAngle;
            remainingDelay = startDelay;
            body.rotation = WorldRotation();
        }
        private void FixedUpdate()
        {
            float step = Time.fixedDeltaTime;
            if (remainingDelay > 0f)
            {
                float wait = Mathf.Min(remainingDelay, step);
                remainingDelay -= wait;
                step -= wait;
            }
            angle = Mathf.Repeat(angle + degreesPerSecond * step, 360f);
            body.MoveRotation(WorldRotation());
        }
        private Quaternion WorldRotation()
        {
            Vector3 axis = localAxis.sqrMagnitude > 0.0001f ? localAxis.normalized : Vector3.forward;
            Quaternion parentRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            return parentRotation * restLocalRotation * Quaternion.AngleAxis(angle, axis);
        }
        private void OnValidate()
        {
            if (localAxis.sqrMagnitude < 0.0001f) localAxis = Vector3.forward;
            startDelay = Mathf.Max(0f, startDelay);
        }
    }
}