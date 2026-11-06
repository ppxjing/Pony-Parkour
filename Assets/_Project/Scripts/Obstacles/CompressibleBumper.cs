using System.Collections.Generic;
using Adventure.AI;
using Adventure.Player;
using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider))]
    public class CompressibleBumper : MonoBehaviour
    {
        [Header("Launch at reference size")]
        [SerializeField, Min(0f)] private float horizontalSpeed = 7f;
        [SerializeField, Min(0f)] private float upwardSpeed = 4f;
        [Tooltip("World diameter at which the configured launch speeds apply. Twice the resting diameter gives twice the speed.")]
        [SerializeField, Min(0.01f)] private float referenceDiameter = 0.5f;
        [SerializeField, Min(0f)] private float controlLockSeconds = 0.25f;
        [SerializeField, Min(0.05f)] private float hitCooldown = 0.6f;
        [Header("Compression and inflation")]
        [SerializeField, Range(0.1f, 1f)] private float shrunkenScale = 0.75f;
        [SerializeField, Min(0.01f)] private float shrinkSeconds = 0.12f;
        [SerializeField, Min(1f)] private float inflatedScale = 1.18f;
        [SerializeField, Min(0.01f)] private float inflateSeconds = 0.08f;
        [SerializeField, Min(0.01f)] private float recoverSeconds = 0.3f;

        private struct PendingRacer
        {
            public ThirdPersonMotor player;
            public RacerCollisionBody ai;
            public Vector3 direction;
        }

        private readonly List<PendingRacer> pending = new List<PendingRacer>(4);
        private Collider solid;
        private Vector3 restScale;
        private float pulseTime;
        private float nextHitTime;
        private float launchSizeMultiplier;
        private bool initialized, animating, released;
        public int BumpCount { get; private set; }
        public int LaunchCount { get; private set; }
        public float LaunchSizeMultiplier => animating ? launchSizeMultiplier : RestingSizeMultiplier();

        protected virtual void Awake()
        {
            solid = GetComponent<Collider>();
            restScale = transform.localScale;
            initialized = true;
        }

        private float RestingSizeMultiplier()
        {
            Vector3 scale = transform.lossyScale;
            float largestAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            float diameter;
            if (solid is SphereCollider sphere) diameter = 2f * sphere.radius * largestAxis;
            else if (solid is MeshCollider mesh && mesh.sharedMesh != null)
            {
                Vector3 size = Vector3.Scale(mesh.sharedMesh.bounds.size, scale);
                diameter = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            }
            else
            {
                Vector3 size = solid.bounds.size;
                diameter = Mathf.Max(size.x, size.y, size.z);
            }
            return diameter / Mathf.Max(0.01f, referenceDiameter);
        }

        public bool TryBump(ThirdPersonMotor player, Vector3 contactNormal)
        {
            if (player == null || !player.CanReceiveRacerImpact) return false;
            return QueueRacer(player, null, player.transform, contactNormal);
        }

        public bool TryBump(RacerCollisionBody ai)
        {
            if (ai == null || !ai.CanReceiveObstacleImpact) return false;
            return QueueRacer(null, ai, ai.transform, ai.transform.position - transform.position);
        }

        private bool QueueRacer(ThirdPersonMotor player, RacerCollisionBody ai, Transform racer, Vector3 normal)
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0f || !solid.enabled || solid.isTrigger) return false;
            if (animating && released) return false;
            if (!animating && Time.time < nextHitTime) return false;
            for (int i = 0; i < pending.Count; i++)
                if ((player != null && pending[i].player == player) || (ai != null && pending[i].ai == ai)) return false;

            if (!animating)
            {
                // Sample before compression so neither the animation nor queued contacts amplify the force.
                restScale = transform.localScale;
                launchSizeMultiplier = RestingSizeMultiplier();
                pulseTime = 0f;
                released = false;
                animating = true;
                pending.Clear();
                BumpCount++;
            }
            Vector3 away = Vector3.ProjectOnPlane(normal, Vector3.up);
            if (away.sqrMagnitude < 0.001f) away = Vector3.ProjectOnPlane(racer.position - transform.position, Vector3.up);
            if (away.sqrMagnitude < 0.001f) away = -racer.forward;
            pending.Add(new PendingRacer { player = player, ai = ai, direction = away.normalized });
            return true;
        }

        protected virtual void Update()
        {
            if (!animating || Time.deltaTime <= 0f) return;
            pulseTime += Time.deltaTime;
            float releaseTime = shrinkSeconds + inflateSeconds;
            float factor;
            if (pulseTime < shrinkSeconds)
                factor = Mathf.Lerp(1f, shrunkenScale, Mathf.SmoothStep(0f, 1f, pulseTime / shrinkSeconds));
            else if (pulseTime < releaseTime)
                factor = Mathf.Lerp(shrunkenScale, inflatedScale, Mathf.SmoothStep(0f, 1f, (pulseTime - shrinkSeconds) / inflateSeconds));
            else
                factor = Mathf.Lerp(inflatedScale, 1f, Mathf.SmoothStep(0f, 1f, (pulseTime - releaseTime) / recoverSeconds));
            transform.localScale = restScale * factor;

            if (!released && pulseTime >= releaseTime)
            {
                released = true;
                nextHitTime = Time.time + hitCooldown;
                foreach (PendingRacer racer in pending)
                {
                    Vector3 velocity = (racer.direction * horizontalSpeed + Vector3.up * upwardSpeed) * launchSizeMultiplier;
                    bool launched = racer.player != null
                        ? racer.player.TryReceiveRacerImpact(velocity, controlLockSeconds, hitCooldown)
                        : racer.ai != null && racer.ai.TryReceiveObstacleImpact(velocity, hitCooldown);
                    if (launched) LaunchCount++;
                }
                pending.Clear();
            }
            if (pulseTime >= releaseTime + recoverSeconds)
            {
                transform.localScale = restScale;
                animating = false;
            }
        }

        protected virtual void OnDisable()
        {
            if (initialized && animating) transform.localScale = restScale;
            pending.Clear();
            animating = released = false;
            nextHitTime = 0f;
        }
    }
}