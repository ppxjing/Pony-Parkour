using Adventure.Player;
using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public sealed class BeachPushableBottle : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float pushImpulse = 2.2f;
        [SerializeField, Min(0f)] private float liftImpulse = 0.9f;
        [SerializeField, Min(0f)] private float tumbleImpulse = 0.25f;
        [SerializeField, Min(0f)] private float minimumPlayerSpeed = 0.5f;
        [SerializeField, Min(0.05f)] private float pushCooldown = 0.25f;
        [SerializeField, Min(0.1f)] private float maxPushSpeed = 12f;
        private Rigidbody body;
        private float nextPushTime;
        public int PushCount { get; private set; }

        private void Awake() { body = GetComponent<Rigidbody>(); }

        public bool TryPush(ThirdPersonMotor player, Vector3 contactPoint, Vector3 moveDirection)
        {
            if (!isActiveAndEnabled || player == null || !player.isActiveAndEnabled ||
                body.isKinematic || Time.timeScale <= 0f || Time.time < nextPushTime) return false;
            float speed = player.PlanarVelocity.magnitude;
            if (speed < minimumPlayerSpeed || body.linearVelocity.sqrMagnitude >= maxPushSpeed * maxPushSpeed) return false;
            Vector3 direction = Vector3.ProjectOnPlane(moveDirection, Vector3.up);
            if (direction.sqrMagnitude < 0.001f) return false;
            direction.Normalize();
            float strength = Mathf.Clamp(speed / 5f, 0.4f, 1.5f);
            body.AddForceAtPosition((direction * pushImpulse + Vector3.up * liftImpulse) * strength, contactPoint, ForceMode.Impulse);
            body.AddTorque(Vector3.Cross(Vector3.up, direction) * tumbleImpulse * strength, ForceMode.Impulse);
            nextPushTime = Time.time + pushCooldown;
            PushCount++;
            return true;
        }
    }
}