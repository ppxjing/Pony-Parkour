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
        private Vector3 destination;

        public int CurrentWaypointIndex { get; private set; }
        public int CompletedLoops { get; private set; }
        public IReadOnlyList<Transform> Waypoints => waypoints;
        public bool IsWaiting { get; private set; }
        public bool IsFinished => finished;
        public float RemainingWaitTime => Mathf.Max(0f, waitRemaining);

        public void BeginRaceAtWaypoint(int waypointIndex)
        {
            if (waypoints.Count == 0) return;
            CurrentWaypointIndex = Mathf.Clamp(waypointIndex, 0, waypoints.Count - 1);
            hasDestination = false;
            finished = false;
            reportedFailure = false;
            IsWaiting = false;
            waitRemaining = 0f;
            retryRemaining = 0f;
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
            retryRemaining = 0f;
            originalStoppingDistance = agent.stoppingDistance;
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = false;
        }

        private void Update()
        {
            if (finished || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
                return;

            if (IsWaiting)
            {
                // This component owns the stop during the dwell; don't let isStopped freeze its timer.
                waitRemaining = Mathf.Max(0f, waitRemaining - Time.deltaTime);
                if (waitRemaining <= 0f)
                    AdvanceWaypoint();
                return;
            }

            if (agent.isStopped || traversal.IsTraversing || agent.isOnOffMeshLink)
                return;
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
    }
}
