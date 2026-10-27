
using Adventure.Obstacles;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Adventure.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(NavMeshLinkTraversal))]
    public sealed class NavMeshLoopPatrol : MonoBehaviour
    {
        [Tooltip("按顺序拖入场景途经点或 Link End。带 AIPatrolWaypoint 的点使用自己的停留时间；不可达目标不会跳过。")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [Tooltip("普通 Transform 目标的停留秒数；专用途经点使用自己的 Wait Duration。")]
        [SerializeField, Min(0f)] private float waypointPause = 0.25f;
        [SerializeField] private bool loop = true;

        private NavMeshAgent agent;
        private NavMeshLinkTraversal traversal;
        private bool hasDestination;
        private bool finished;
        private bool reportedFailure;
        private float waitRemaining;
        private float retryRemaining;
        private float originalStoppingDistance;
        private float arrivalDistance;
        private float arrivalPause;
        private Transform currentTarget;
        private Vector3 targetPosition;
        
        private float obstacleRespawnCooldownUntil;
        private int lastCompletedWaypointIndex;
        [SerializeField, Min(1f)] private float stuckTimeout = 5f;
        [SerializeField, Min(0.1f)] private float stuckMovementThreshold = 0.75f;
        private Vector3 watchdogPosition;
        private float watchdogLastProgressTime;
        private int watchdogWaypointIndex;
private Vector3 destination;

        public int CurrentWaypointIndex { get; private set; }
        public int CompletedLoops { get; private set; }
        public IReadOnlyList<Transform> Waypoints => waypoints;
        public bool IsWaiting { get; private set; }
        public bool IsFinished => finished;
        
        public bool IsObstacleStunned => false;
public float RemainingWaitTime => Mathf.Max(0f, waitRemaining);

        public void BeginRaceAtWaypoint(int waypointIndex)
        {
            if (waypoints.Count == 0) return;
            CurrentWaypointIndex = Mathf.Clamp(waypointIndex, 0, waypoints.Count - 1);
            lastCompletedWaypointIndex = (CurrentWaypointIndex - 1 + waypoints.Count) % waypoints.Count;
            hasDestination = false;
            finished = false;
            reportedFailure = false;
            IsWaiting = false;
            waitRemaining = 0f;
            retryRemaining = 0f;
            ResetStuckWatchdog();
            
if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = false;
            }
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            traversal = GetComponent<NavMeshLinkTraversal>();
        }

        private void OnEnable()
        {
            CurrentWaypointIndex = 0;
            CompletedLoops = 0;
            hasDestination = false;
            finished = false;
            reportedFailure = false;
            IsWaiting = false;
            waitRemaining = 0f;
            
            obstacleRespawnCooldownUntil = 0f;
            lastCompletedWaypointIndex = 0;
retryRemaining = 0f;
            ResetStuckWatchdog();
            
originalStoppingDistance = agent.stoppingDistance;
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = false;
        }

        private void Update()
        {
            

            if (finished || !agent.isActiveAndEnabled) return;

            if (!agent.isOnNavMesh)
            {
                if (Time.time - watchdogLastProgressTime >= 1f)
                    RespawnAtLastCheckpoint();
                return;
            }

            if (IsWaiting)
            {
                ResetStuckWatchdog();
                // This component owns the stop during the dwell; don't let isStopped freeze its timer.
                waitRemaining = Mathf.Max(0f, waitRemaining - Time.deltaTime);
                if (waitRemaining <= 0f)
                    AdvanceWaypoint();
                return;
            }

            if (agent.isStopped)
            {
                ResetStuckWatchdog();
                return;
            }
            if (RecoverIfStuck()) return;
            if (traversal.IsTraversing || agent.isOnOffMeshLink) return;
            if (retryRemaining > 0f)
            {
                retryRemaining -= Time.deltaTime;
                return;
            }
            if (waypoints.Count == 0)
            {
                RetryLater("Patrol Waypoints is empty. Add scene waypoint instances to the list.");
                return;
            }
            if (CurrentWaypointIndex >= waypoints.Count)
                CurrentWaypointIndex = 0;

            Transform target = waypoints[CurrentWaypointIndex];
            if (target == null)
            {
                RetryLater("Missing required patrol waypoint at list index " + CurrentWaypointIndex + ". It will not be skipped.");
                return;
            }

            if (hasDestination && (target != currentTarget || (target.position - targetPosition).sqrMagnitude > 0.0001f))
            {
                agent.ResetPath();
                hasDestination = false;
            }
            if (!hasDestination)
            {
                BeginDestination(target);
                return;
            }
            if (agent.pathPending)
                return;
            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                RetryLater("Required waypoint '" + target.name + "' is unreachable. Check NavMesh, forbidden zones, Links and Area Mask. It will not be skipped.");
                return;
            }

            float remaining = agent.remainingDistance;
            if (float.IsInfinity(remaining) || float.IsNaN(remaining) || remaining > arrivalDistance)
                return;
            Vector3 feet = transform.position - Vector3.up * agent.baseOffset;
            if (Vector3.Distance(feet, destination) > arrivalDistance)
                return;

            reportedFailure = false;
            hasDestination = false;
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            IsWaiting = true;
            waitRemaining = arrivalPause;
            if (waitRemaining <= 0f)
                AdvanceWaypoint();
        }

        public void ResumeAfterImpact()
        {
            // Keep waypoint order, completed laps and an unfinished waypoint pause.
            hasDestination = false;
            currentTarget = null;
            retryRemaining = 0f;
            ResetStuckWatchdog();
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = finished || IsWaiting;
            }
        }
        private void BeginDestination(Transform target)
        {
            var settings = target.GetComponent<AIPatrolWaypoint>();
            destination = target.position;
            arrivalDistance = Mathf.Max(0.12f, originalStoppingDistance);
            arrivalPause = Mathf.Max(0f, waypointPause);
            agent.stoppingDistance = originalStoppingDistance;

            if (settings != null)
            {
                if (!settings.isActiveAndEnabled)
                {
                    RetryLater("Required waypoint '" + target.name + "' is disabled. Enable it or remove it from Waypoints.");
                    return;
                }
                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                NavMeshHit hit;
                if (!NavMesh.SamplePosition(target.position, out hit, settings.NavMeshSampleDistance, filter))
                {
                    RetryLater("Required waypoint '" + target.name + "' is outside the NavMesh. Move it onto a walkable surface.");
                    return;
                }
                destination = hit.position;
                arrivalDistance = settings.ArrivalDistance;
                arrivalPause = settings.WaitDuration;
                // A large Agent stopping distance must not prevent reaching a mandatory point.
                agent.stoppingDistance = Mathf.Min(originalStoppingDistance, arrivalDistance * 0.5f);
            }
            if (!agent.SetDestination(destination))
            {
                RetryLater("Cannot set required waypoint '" + target.name + "' as a destination.");
                return;
            }
            currentTarget = target;
            targetPosition = target.position;
            hasDestination = true;
        }

        private void AdvanceWaypoint()
        {
            IsWaiting = false;
            waitRemaining = 0f;
            agent.stoppingDistance = originalStoppingDistance;
            ResetStuckWatchdog();
            
lastCompletedWaypointIndex = CurrentWaypointIndex;
            CurrentWaypointIndex++;
            if (CurrentWaypointIndex >= waypoints.Count)
            {
                CompletedLoops++;
                if (loop)
                    CurrentWaypointIndex = 0;
                else
                    finished = true;
            }
            agent.isStopped = finished;
        }

        private void RetryLater(string message)
        {
            hasDestination = false;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.stoppingDistance = originalStoppingDistance;
            retryRemaining = 1f;
            if (reportedFailure)
                return;
            Debug.LogWarning(message, this);
            reportedFailure = true;
        }

        private void OnDisable()
        {
            IsWaiting = false;
            if (agent == null) return;
            agent.stoppingDistance = originalStoppingDistance;
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = true;
        }
    

private void ResetStuckWatchdog()
        {
            watchdogPosition = transform.position;
            watchdogLastProgressTime = Time.time;
            watchdogWaypointIndex = CurrentWaypointIndex;
        }

        private bool RecoverIfStuck()
        {
            if (watchdogWaypointIndex != CurrentWaypointIndex ||
                (transform.position - watchdogPosition).sqrMagnitude >= stuckMovementThreshold * stuckMovementThreshold)
            {
                ResetStuckWatchdog();
                return false;
            }

            if (Time.time - watchdogLastProgressTime < stuckTimeout) return false;
            bool recovered = RespawnAtLastCheckpoint();
            if (recovered)
                Debug.LogWarning(name + " was stuck and returned to its last checkpoint.", this);
            else
                watchdogLastProgressTime = Time.time;
            return recovered;
        }

        public bool TryGetLastCheckpointPose(out Vector3 position, out Quaternion rotation)
        {
            position = transform.position;
            rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            if (agent == null || waypoints.Count == 0) return false;
            int checkpointIndex = Mathf.Clamp(lastCompletedWaypointIndex, 0, waypoints.Count - 1);
            Transform checkpoint = waypoints[checkpointIndex];
            if (checkpoint == null) return false;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(checkpoint.position, out NavMeshHit hit, 4f, filter)) return false;
            position = hit.position;
            Transform next = waypoints[Mathf.Clamp(CurrentWaypointIndex, 0, waypoints.Count - 1)];
            if (next != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(next.position - position, Vector3.up);
                if (forward.sqrMagnitude > 0.001f) rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
            return true;
        }

        public void ResumeAfterDeath()
        {
            IsWaiting = false;
            waitRemaining = 0f;
            reportedFailure = false;
            // RacerCollisionBody owns the configurable death protection period.
            obstacleRespawnCooldownUntil = Time.time;
            ResumeAfterImpact();
        }

        private bool RespawnAtLastCheckpoint()
        {
            if (agent == null || !agent.isActiveAndEnabled ||
                !TryGetLastCheckpointPose(out Vector3 position, out Quaternion rotation)) return false;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            if (!agent.Warp(position) || !agent.isOnNavMesh) return false;
            transform.rotation = rotation;

            hasDestination = false;
            IsWaiting = false;
            waitRemaining = 0f;
            retryRemaining = 0f;
            currentTarget = null;
            obstacleRespawnCooldownUntil = Time.time + 0.5f;
            agent.velocity = Vector3.zero;
            agent.isStopped = false;
            ResetStuckWatchdog();
            return true;
        }


private static bool IsGameplayObstacle(Collider other)
        {
            return other.GetComponentInParent<InflatingBounceBall>() != null ||
                   other.GetComponentInParent<RotatingPillarRod>() != null ||
                   other.GetComponentInParent<HammerHeadDamage>() != null ||
                   (other.GetComponentInParent<DeathZone>() is DeathZone zone && zone.isActiveAndEnabled);
        }


private void OnTriggerStay(Collider other)
        {
            if (IsGameplayObstacle(other)) ApplyObstacleHit(other.transform.position);
        }


private void OnTriggerEnter(Collider other)
        {
            if (IsGameplayObstacle(other)) ApplyObstacleHit(other.transform.position);
        }


public void ApplyObstacleHit(float stunDuration = 1.25f)
        {
            ApplyObstacleHit(transform.position);
        }

        public void ApplyObstacleHit(Vector3 sourcePosition)
        {
            if (finished || Time.time < obstacleRespawnCooldownUntil) return;
            var collisionBody = GetComponent<RacerCollisionBody>();
            if (collisionBody != null && collisionBody.isActiveAndEnabled)
                collisionBody.RequestDeath(sourcePosition);
            else
                RespawnAtLastCheckpoint();
        }
}
}
