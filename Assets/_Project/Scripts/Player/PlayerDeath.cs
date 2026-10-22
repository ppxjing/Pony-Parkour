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
        private Vector3[] restPositions;
        private Quaternion[] restRotations;
        private bool animatorWasEnabled;
        private float protectedUntil;
        public bool IsDead { get; private set; }
        public bool IsAlive => !IsDead && !deathRequested;
        public float DiedAt { get; private set; }

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
            restPositions = new Vector3[ragdollBodies.Length];
            restRotations = new Quaternion[ragdollBodies.Length];
            animatorWasEnabled = animator != null && animator.enabled;
            for (int i = 0; i < ragdollBodies.Length; i++)
            {
                var body = ragdollBodies[i];
                if (body == null) continue;
                restPositions[i] = body.transform.localPosition;
                restRotations[i] = body.transform.localRotation;
                body.isKinematic = true;
                body.useGravity = false;
            }
            foreach (var collider in ragdollColliders)
                if (collider != null) collider.enabled = false;
        }

        public void RequestDeath(Vector3 sourcePosition)
        {
            if (!enabled || IsDead || deathRequested || Time.time < protectedUntil) return;
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
            DiedAt = Time.time;
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

        public void Revive(Vector3 position, Quaternion rotation, float protectionTime)
        {
            if (!enabled || restPositions == null) return;
            input.enabled = false;
            motor.enabled = false;
            controller.enabled = false;
            foreach (var collider in ragdollColliders)
                if (collider != null) collider.enabled = false;
            foreach (var body in ragdollBodies)
            {
                if (body == null) continue;
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
                body.useGravity = false;
            }
            motor.TeleportAndReset(position, rotation);
            for (int i = 0; i < ragdollBodies.Length; i++)
            {
                var body = ragdollBodies[i];
                if (body == null) continue;
                body.transform.localPosition = restPositions[i];
                body.transform.localRotation = restRotations[i];
            }
            foreach (var body in ragdollBodies)
            {
                if (body == null) continue;
                body.position = body.transform.position;
                body.rotation = body.transform.rotation;
            }
            if (animator != null && animatorWasEnabled)
            {
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
            }
            IsDead = false;
            deathRequested = false;
            inheritedVelocity = Vector3.zero;
            protectedUntil = Time.time + Mathf.Max(0f, protectionTime);
            Physics.SyncTransforms();
            controller.enabled = true;
            input.enabled = true;
            motor.enabled = true;
            orbitCamera.RestorePlayerControl();
        }
    }
}
