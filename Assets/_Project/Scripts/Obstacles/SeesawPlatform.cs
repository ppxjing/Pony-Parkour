using System.Collections.Generic;
using Adventure.Player;
using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SeesawPlatform : MonoBehaviour
    {
        [Tooltip("Maximum tilt to either side, in degrees.")]
        [SerializeField, Range(1f, 40f)] private float maxAngle = 25f;
        [Tooltip("Angular acceleration from one player at the end of the board, in degrees/s².")]
        [SerializeField, Min(0f)] private float loadAcceleration = 120f;
        [Tooltip("Rotational resistance. Higher values make tilting slower and reduce oscillation.")]
        [SerializeField, Min(0f)] private float angularDamping = 6f;
        [Tooltip("Restoring acceleration per degree. Zero disables automatic return to level.")]
        [SerializeField, Min(0f)] private float returnStrength = 2f;
        [Tooltip("Maximum rotation speed in degrees per second.")]
        [SerializeField, Min(1f)] private float maxAngularSpeed = 35f;
        [Tooltip("Distance from the pivot to an end, in local units. Match this to the model.")]
        [SerializeField, Min(0.1f)] private float halfLength = 1.8f;
        [Tooltip("Foot positions within this local distance of the pivot add no turning force.")]
        [SerializeField, Min(0f)] private float centerDeadZone = 0.1f;
        private readonly List<ThirdPersonMotor> riders = new List<ThirdPersonMotor>();
        private Rigidbody body;
        private Quaternion restLocalRotation;
        private float angularVelocity;
        public float AngleDegrees { get; private set; }
        public float AngularSpeed => angularVelocity;
        public int RiderCount => riders.Count;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            restLocalRotation = transform.localRotation;
        }
        public void RegisterSupport(ThirdPersonMotor player)
        {
            if (isActiveAndEnabled && !riders.Contains(player)) riders.Add(player);
        }
        private void FixedUpdate()
        {
            float load = 0f;
            for (int i = riders.Count - 1; i >= 0; i--)
            {
                ThirdPersonMotor rider = riders[i];
                if (rider == null || !rider.isActiveAndEnabled || rider.StandingPlatform != this)
                { riders.RemoveAt(i); continue; }
                float lever = transform.InverseTransformPoint(rider.FootPosition).x;
                if (Mathf.Abs(lever) <= centerDeadZone) continue;
                load -= Mathf.Clamp(lever / halfLength, -1f, 1f);
            }
            // Inertial rotation with viscous damping, about the fixed local Z axle.
            // Substeps keep high damping/return values stable at normal physics rates.
            float remaining = Time.fixedDeltaTime;
            while (remaining > 0f)
            {
                float dt = Mathf.Min(remaining, 0.005f);
                float acceleration = load * loadAcceleration - returnStrength * AngleDegrees;
                angularVelocity = (angularVelocity + acceleration * dt) / (1f + angularDamping * dt);
                angularVelocity = Mathf.Clamp(angularVelocity, -maxAngularSpeed, maxAngularSpeed);
                AngleDegrees += angularVelocity * dt;
                if (AngleDegrees >= maxAngle)
                { AngleDegrees = maxAngle; angularVelocity = Mathf.Min(0f, angularVelocity); }
                if (AngleDegrees <= -maxAngle)
                { AngleDegrees = -maxAngle; angularVelocity = Mathf.Max(0f, angularVelocity); }
                remaining -= dt;
            }
            Quaternion parent = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            body.MoveRotation(parent * restLocalRotation * Quaternion.AngleAxis(AngleDegrees, Vector3.forward));
        }
        private void OnDisable()
        {
            riders.Clear();
            angularVelocity = 0f;
        }
        private void OnValidate()
        {
            maxAngle = Mathf.Clamp(maxAngle, 1f, 40f);
            halfLength = Mathf.Max(0.1f, halfLength);
            centerDeadZone = Mathf.Clamp(centerDeadZone, 0f, halfLength);
            loadAcceleration = Mathf.Max(0f, loadAcceleration);
            angularDamping = Mathf.Max(0f, angularDamping);
            returnStrength = Mathf.Max(0f, returnStrength);
            maxAngularSpeed = Mathf.Max(1f, maxAngularSpeed);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position - transform.forward, transform.position + transform.forward);
        }
    }
}