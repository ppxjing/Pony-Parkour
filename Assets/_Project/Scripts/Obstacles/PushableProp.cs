using Adventure.Player;
using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public class PushableProp : MonoBehaviour
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

        protected virtual void Awake() { body = GetComponent<Rigidbody>(); }

        public bool TryPush(ThirdPersonMotor player, Vector3 contactPoint, Vector3 moveDirection)
        {
            if (!isActiveAndEnabled || player == null || !player.isActiveAndEnabled ||
                body.isKinematic || Time.timeScale <= 0f || Time.time < nextPushTime) return false;
            float speed = player.PlanarVelocity.magnitude;
            if (speed < minimumPlayerSpeed || body.linearVelocity.sqrMagnitude >= maxPushSpeed * maxPushSpeed) return false;
            Vector3 direction = Vector3.ProjectOnPlane(moveDirection, Vector3.up);
            // Auto-stepping onto a low plate reports a downward hit; retain the player's forward momentum.
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.ProjectOnPlane(player.PlanarVelocity, Vector3.up);
            if (direction.sqrMagnitude < 0.001f) return false;
            direction.Normalize();
            float strength = Mathf.Clamp(speed / 5f, 0.4f, 1.5f);
            Vector3 impulse = (direction * pushImpulse + Vector3.up * liftImpulse) * strength;
            // Very light props should not receive unbounded velocity from a character controller.
            impulse = Vector3.ClampMagnitude(impulse, body.mass * Mathf.Max(0f, maxPushSpeed - body.linearVelocity.magnitude));
            body.AddForceAtPosition(impulse, contactPoint, ForceMode.Impulse);
            body.AddTorque(Vector3.Cross(Vector3.up, direction) * Mathf.Min(tumbleImpulse * strength, body.mass * 2f), ForceMode.Impulse);
            nextPushTime = Time.time + pushCooldown;
            PushCount++;
            return true;
        }
    }
}