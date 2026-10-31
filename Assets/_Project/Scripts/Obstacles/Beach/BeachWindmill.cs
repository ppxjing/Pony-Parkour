using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent]
    public sealed class BeachWindmill : MonoBehaviour
    {
        [SerializeField] private Rigidbody blades;
        [Tooltip("Axis in the blade model's initial local space. This asset uses X.")]
        [SerializeField] private Vector3 localAxis = Vector3.right;
        [SerializeField, Min(0f)] private float degreesPerSecond = 90f;
        [SerializeField] private bool reverseDirection;
        private Quaternion restLocalRotation;
        private float angle;

        private void Awake()
        {
            if (blades == null) { Debug.LogError("Assign the windmill blade Rigidbody.", this); enabled = false; return; }
            restLocalRotation = blades.transform.localRotation;
        }

        private void FixedUpdate()
        {
            angle = Mathf.Repeat(angle + degreesPerSecond * (reverseDirection ? -1f : 1f) * Time.fixedDeltaTime, 360f);
            Quaternion parent = blades.transform.parent != null ? blades.transform.parent.rotation : Quaternion.identity;
            Vector3 axis = localAxis.sqrMagnitude > 0.0001f ? localAxis.normalized : Vector3.right;
            blades.MoveRotation(parent * restLocalRotation * Quaternion.AngleAxis(angle, axis));
        }
    }
}