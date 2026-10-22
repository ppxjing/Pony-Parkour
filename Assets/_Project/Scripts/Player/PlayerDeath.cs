using Adventure.Obstacles;
using UnityEngine;

namespace Adventure.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(ThirdPersonMotor), typeof(PlayerInputReader))]
    public sealed class PlayerDeath : MonoBehaviour
    {
        [Header("Ragdoll references")]
        [SerializeField] private Rigidbody[] ragdollBodies;
        [SerializeField] private Collider[] ragdollColliders;
        [SerializeField] private Rigidbody impulseBody;
        [SerializeField] private Animator animator;
        [SerializeField] private ThirdPersonOrbitCamera orbitCamera;
        [SerializeField] private Transform ragdollCameraTarget;
        [Header("Death reaction")]
        [SerializeField, Min(0f)] private float knockbackSpeed = 3f;
        [SerializeField, Min(0f)] private float upwardSpeed = 1.5f;
        [SerializeField, Min(0f)] private float tumbleSpeed = 5f;
        private CharacterController controller;
        private ThirdPersonMotor motor;
        private PlayerInputReader input;
        private bool deathRequested;
        private Vector3 hazardPosition;
        private Vector3 inheritedVelocity;
        public bool IsDead { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            motor = GetComponent<ThirdPersonMotor>();
            input = GetComponent<PlayerInputReader>();
            if (impulseBody == null || ragdollBodies == null || ragdollBodies.Length == 0 ||
                ragdollColliders == null || ragdollColliders.Length == 0 ||
                orbitCamera == null || ragdollCameraTarget == null)
            {
                Debug.LogError("PlayerDeath: assign ragdoll bodies/colliders, impulse body and camera references.", this);
                enabled = false;
                return;
            }
            foreach (var body in ragdollBodies)
            {
                if (body == null) continue;
                body.isKinematic = true;
                body.useGravity = false;
            }
            foreach (var collider in ragdollColliders)
                if (collider != null) collider.enabled = false;
        }

        public void RequestDeath(Vector3 sourcePosition)
        {
            if (!enabled || IsDead || deathRequested) return;
            deathRequested = true;
            hazardPosition = sourcePosition;
            inheritedVelocity = controller.velocity;
            input.enabled = false;
            motor.enabled = false;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var hazard = hit.collider.GetComponentInParent<LethalObstacle>();
            if (hazard != null && hazard.isActiveAndEnabled) RequestDeath(hazard.transform.position);
        }

        private void LateUpdate()
        {
            // Finish CharacterController.Move before swapping its collision representation.
            if (!deathRequested || IsDead) return;
            IsDead = true;
            controller.enabled = false;
            if (animator != null) animator.enabled = false;
            foreach (var collider in ragdollColliders)
                if (collider != null) collider.enabled = true;
            foreach (var body in ragdollBodies)
            {
                if (body == null) continue;
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = inheritedVelocity;
                body.WakeUp();
            }
            Vector3 away = Vector3.ProjectOnPlane(impulseBody.worldCenterOfMass - hazardPosition, Vector3.up);
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            away.Normalize();
            impulseBody.AddForce(away * knockbackSpeed + Vector3.up * upwardSpeed, ForceMode.VelocityChange);
            impulseBody.AddTorque(Vector3.Cross(Vector3.up, away) * tumbleSpeed, ForceMode.VelocityChange);
            orbitCamera.FollowWithoutInput(ragdollCameraTarget);
        }
    }
}
