using Adventure.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Adventure.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class RacerCollisionBody : MonoBehaviour
    {
        [Header("Player / AI impact")]
        [SerializeField, Min(0.1f)] private float minimumClosingSpeed = 0.75f;
        [SerializeField, Min(0f)] private float impactMultiplier = 1.1f;
        [SerializeField, Min(0f)] private float minimumLaunchSpeed = 3f;
        [SerializeField, Min(0f)] private float maximumLaunchSpeed = 8f;
        [SerializeField, Min(0f)] private float upwardSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float collisionCooldown = 0.8f;
        [SerializeField, Min(0f)] private float playerControlLockTime = 0.2f;
        [Header("AI flight and recovery")]
        [SerializeField, Min(0.1f)] private float gravity = 25f;
        [SerializeField, Min(0.5f)] private float maximumFlightTime = 3f;
        [SerializeField, Min(0.05f)] private float landingSampleDistance = 0.6f;
        [Tooltip("额外接触余量，避免 CharacterController 先挤开后漏判 AI 主动碰撞。")]
        [SerializeField, Min(0.02f)] private float contactPadding = 0.12f;
        [SerializeField] private CapsuleCollider contactSensor;

        [Header("AI death ragdoll")]
        [SerializeField, Min(0f)] private float deathRespawnDelay = 2f;
        [SerializeField, Min(0f)] private float deathProtectionTime = 1f;
        [SerializeField, Min(0f)] private float deathKnockbackSpeed = 3f;
        [SerializeField, Min(0f)] private float deathUpwardSpeed = 1.5f;
        [SerializeField, Min(0f)] private float deathTumbleSpeed = 5f;

        private NavMeshAgent agent;
        private NavMeshLoopPatrol patrol;
        private NavMeshLinkTraversal traversal;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private float nextImpactTime, launchTime, groundContactTime = float.NegativeInfinity;
        private Vector3 lastSafePosition;
        private Quaternion lastSafeRotation;
        private float protectedUntil, nextDeathRecoveryAttempt;
        private Animator[] deathAnimators;
        private bool[] animatorWasEnabled;
        public bool IsDead { get; private set; }
        public int DeathCount { get; private set; }
        public int DeathRespawnCount { get; private set; }
        public bool IsKnockedBack { get; private set; }
        public int ImpactCount { get; private set; }
        public bool CanReceiveObstacleImpact => isActiveAndEnabled && !IsDead && !IsKnockedBack &&
            Time.time >= nextImpactTime && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh &&
            (patrol == null || !patrol.IsFinished);

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            patrol = GetComponent<NavMeshLoopPatrol>();
            traversal = GetComponent<NavMeshLinkTraversal>();
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
            ConfigureCollision();
        }

        public void ConfigureCollision()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (capsule == null) capsule = GetComponent<CapsuleCollider>();
            Vector3 scale = transform.lossyScale;
            float radialScale = Mathf.Max(0.001f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
            float heightScale = Mathf.Max(0.001f, Mathf.Abs(scale.y));
            capsule.direction = 1;
            capsule.isTrigger = false;
            capsule.radius = agent.radius / radialScale;
            capsule.height = Mathf.Max(capsule.radius * 2f, agent.height / heightScale);
            capsule.center = Vector3.up * (capsule.height * 0.5f);
            if (contactSensor == null)
            {
                var sensor = new GameObject("Racer Contact Sensor");
                sensor.transform.SetParent(transform, false);
                contactSensor = sensor.AddComponent<CapsuleCollider>();
            }
            // The narrow sensor catches an AI running into a stationary controller.
            // The solid capsule continues to provide real world / player contacts.
            contactSensor.isTrigger = true;
            contactSensor.direction = 1;
            contactSensor.center = capsule.center;
            contactSensor.radius = capsule.radius + contactPadding / radialScale;
            contactSensor.height = capsule.height + 0.05f / heightScale;
        }

        private void OnTriggerEnter(Collider other) { TouchPlayer(other); }
        private void OnTriggerStay(Collider other) { TouchPlayer(other); }
        private void TouchPlayer(Collider other)
        {
            var player = other.GetComponentInParent<ThirdPersonMotor>();
            if (player != null) TryCollideWithPlayer(player);
            var ball = other.GetComponentInParent<Adventure.Obstacles.CompressibleBumper>();
            if (ball == null || other.isTrigger || !CanReceiveObstacleImpact) return;
            // The sensor has padding for player contacts. A ball must touch the actual AI capsule.
            Vector3 separation;
            float penetration;
            if (Physics.ComputePenetration(capsule, capsule.transform.position, capsule.transform.rotation,
                other, other.transform.position, other.transform.rotation, out separation, out penetration))
                ball.TryBump(this);
        }

        public bool TryCollideWithPlayer(ThirdPersonMotor player)
        {
            if (!isActiveAndEnabled || IsDead || IsKnockedBack || Time.time < nextImpactTime ||
                player == null || !player.CanReceiveRacerImpact || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
                return false;
            Vector3 direction = Vector3.ProjectOnPlane(transform.position - player.transform.position, Vector3.up);
            if (direction.sqrMagnitude < 0.0001f) return false;
            direction.Normalize();
            float closingSpeed = Vector3.Dot(player.PlanarVelocity - agent.velocity, direction);
            if (closingSpeed < minimumClosingSpeed) return false;
            float speed = Mathf.Clamp(closingSpeed * impactMultiplier, minimumLaunchSpeed,
                Mathf.Max(minimumLaunchSpeed, maximumLaunchSpeed));
            if (!player.TryReceiveRacerImpact(-direction * speed + Vector3.up * upwardSpeed,
                playerControlLockTime, collisionCooldown)) return false;

            LaunchForImpact(direction * speed + Vector3.up * upwardSpeed, collisionCooldown);
            return true;
        }

        public bool TryReceiveObstacleImpact(Vector3 velocity, float cooldown)
        {
            if (!CanReceiveObstacleImpact || Time.timeScale <= 0f) return false;
            LaunchForImpact(velocity, cooldown);
            return true;
        }

        private void LaunchForImpact(Vector3 velocity, float cooldown)
        {
            if (traversal != null) traversal.SuspendForImpact();
            agent.enabled = false;
            IsKnockedBack = true;
            ImpactCount++;
            nextImpactTime = Time.time + Mathf.Max(0f, cooldown);
            launchTime = Time.time;
            groundContactTime = float.NegativeInfinity;
            body.position = transform.position;
            body.isKinematic = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = velocity;
            body.WakeUp();
        }

        public bool RequestDeath(Vector3 sourcePosition)
        {
            if (!isActiveAndEnabled || IsDead || Time.time < protectedUntil ||
                (patrol != null && patrol.IsFinished)) return false;
            Vector3 inheritedVelocity = IsKnockedBack ? body.linearVelocity :
                (agent.isActiveAndEnabled && agent.isOnNavMesh ? agent.velocity : Vector3.zero);
            if (traversal != null) traversal.SuspendForImpact();
            agent.enabled = false;
            IsKnockedBack = false;
            IsDead = true;
            DeathCount++;
            nextDeathRecoveryAttempt = Time.time + deathRespawnDelay;
            // Models are installed after Awake, so capture their animation state on death.
            deathAnimators = GetComponentsInChildren<Animator>(true);
            animatorWasEnabled = new bool[deathAnimators.Length];
            for (int i = 0; i < deathAnimators.Length; i++)
            {
                animatorWasEnabled[i] = deathAnimators[i].enabled;
                deathAnimators[i].enabled = false;
            }
            Vector3 away = Vector3.ProjectOnPlane(body.worldCenterOfMass - sourcePosition, Vector3.up);
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            away.Normalize();
            body.position = transform.position;
            body.rotation = transform.rotation;
            body.constraints = RigidbodyConstraints.None;
            body.isKinematic = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = inheritedVelocity + away * deathKnockbackSpeed + Vector3.up * deathUpwardSpeed;
            body.angularVelocity = Vector3.Cross(Vector3.up, away) * deathTumbleSpeed;
            body.WakeUp();
            return true;
        }

        private void FixedUpdate()
        {
            if (IsDead)
            {
                body.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
                if (Time.time >= nextDeathRecoveryAttempt)
                {
                    nextDeathRecoveryAttempt = Time.time + 0.5f;
                    TryRespawnFromDeath();
                }
                return;
            }
            if (!IsKnockedBack)
            {
                if (agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isOnOffMeshLink)
                {
                    lastSafePosition = transform.position;
                    lastSafeRotation = transform.rotation;
                }
                return;
            }
            body.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
            bool landed = Time.time - launchTime > 0.2f && body.linearVelocity.y <= 0.5f &&
                Time.time - groundContactTime <= Time.fixedDeltaTime * 2.5f;
            if (landed && TryResumeAt(transform.position, landingSampleDistance, true)) return;
            if (Time.time - launchTime > maximumFlightTime || transform.position.y < lastSafePosition.y - 12f)
            {
                // Falling off the track must not strand an agent outside the NavMesh.
                TryResumeAt(lastSafePosition, 2f, false);
            }
        }

        private bool TryRespawnFromDeath()
        {
            Vector3 position = lastSafePosition;
            Quaternion rotation = lastSafeRotation;
            if (patrol == null || !patrol.TryGetLastCheckpointPose(out position, out rotation))
            {
                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                if (!NavMesh.SamplePosition(lastSafePosition, out NavMeshHit hit, 2f, filter)) return false;
                position = hit.position;
                rotation = lastSafeRotation;
            }
            StopPhysics();
            transform.SetPositionAndRotation(position + Vector3.up * agent.baseOffset, rotation);
            body.position = transform.position;
            body.rotation = rotation;
            agent.enabled = true;
            if (!agent.Warp(position) || !agent.isOnNavMesh)
            {
                agent.enabled = false;
                IsDead = true;
                body.constraints = RigidbodyConstraints.None;
                body.isKinematic = false;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                return false;
            }
            RestoreAnimators();
            protectedUntil = Time.time + deathProtectionTime;
            nextImpactTime = Time.time + collisionCooldown;
            DeathRespawnCount++;
            if (patrol != null) patrol.ResumeAfterDeath();
            return true;
        }

        private void RestoreAnimators()
        {
            if (deathAnimators == null) return;
            for (int i = 0; i < deathAnimators.Length; i++)
                if (deathAnimators[i] != null) deathAnimators[i].enabled = animatorWasEnabled[i];
            deathAnimators = null;
            animatorWasEnabled = null;
        }

        private void OnCollisionEnter(Collision collision) { RecordGround(collision); }
        private void OnCollisionStay(Collision collision) { RecordGround(collision); }
        private void RecordGround(Collision collision)
        {
            if (!IsKnockedBack || collision.collider.GetComponentInParent<RacerCollisionBody>() != null ||
                collision.collider.GetComponentInParent<ThirdPersonMotor>() != null) return;
            for (int i = 0; i < collision.contactCount; i++)
                if (Vector3.Dot(collision.GetContact(i).normal, Vector3.up) > 0.5f)
                { groundContactTime = Time.time; break; }
        }

        private bool TryResumeAt(Vector3 position, float range, bool requireSameFloor)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(position - Vector3.up * agent.baseOffset, out NavMeshHit hit, range, filter))
                return false;
            if (requireSameFloor && Mathf.Abs(hit.position.y + agent.baseOffset - position.y) > 0.25f)
                return false;
            StopPhysics();
            transform.position = hit.position + Vector3.up * agent.baseOffset;
            body.position = transform.position;
            agent.enabled = true;
            agent.Warp(hit.position);
            nextImpactTime = Time.time + collisionCooldown;
            if (patrol != null) patrol.ResumeAfterImpact();
            return true;
        }

        private void StopPhysics()
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            IsKnockedBack = false;
            IsDead = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void OnDisable()
        {
            if ((!IsKnockedBack && !IsDead) || body == null || agent == null) return;
            StopPhysics();
            transform.SetPositionAndRotation(lastSafePosition, lastSafeRotation);
            body.position = transform.position;
            body.rotation = transform.rotation;
            RestoreAnimators();
            agent.enabled = true;
            protectedUntil = Time.time + deathProtectionTime;
            nextImpactTime = Time.time + collisionCooldown;
        }
    }
}