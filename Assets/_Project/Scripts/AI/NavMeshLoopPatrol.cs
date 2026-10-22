using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Adventure.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(NavMeshLinkTraversal))]
    public sealed class NavMeshLoopPatrol : MonoBehaviour
    {
        [Tooltip("Ordered destinations. For this scene use each navigation link's End transform.")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField, Min(0f)] private float waypointPause = 0.25f;
        [SerializeField] private bool loop = true;

        private NavMeshAgent agent;
        private NavMeshLinkTraversal traversal;
        private bool hasDestination;
        private bool finished;
        private bool reportedFailure;
        private float waitRemaining;
        public int CurrentWaypointIndex { get; private set; }
        public int CompletedLoops { get; private set; }

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
            waitRemaining = 0f;
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = false;
        }

        private void Update()
        {
            if (finished || waypoints.Count == 0 || !agent.isActiveAndEnabled || !agent.isOnNavMesh || agent.isStopped)
                return;
            if (waitRemaining > 0f)
            {
                waitRemaining -= Time.deltaTime;
                return;
            }

            if (CurrentWaypointIndex >= waypoints.Count)
                CurrentWaypointIndex = 0;

            if (!hasDestination)
            {
                Transform waypoint = waypoints[CurrentWaypointIndex];
                if (waypoint == null || !agent.SetDestination(waypoint.position))
                {
                    RetryLater("Missing or unreachable patrol waypoint.");
                    return;
                }
                hasDestination = true;
                return;
            }

            if (agent.pathPending || traversal.IsTraversing || agent.isOnOffMeshLink)
                return;

            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                RetryLater("Patrol route is disconnected. Check NavMesh Link endpoints and the agent Area Mask.");
                return;
            }

            if (agent.remainingDistance > Mathf.Max(0.12f, agent.stoppingDistance))
                return;

            reportedFailure = false;
            hasDestination = false;
            waitRemaining = waypointPause;
            CurrentWaypointIndex++;
            if (CurrentWaypointIndex < waypoints.Count)
                return;

            CompletedLoops++;
            CurrentWaypointIndex = 0;
            finished = !loop;
        }

        private void RetryLater(string message)
        {
            hasDestination = false;
            waitRemaining = 1f;
            if (reportedFailure)
                return;
            Debug.LogWarning(message, this);
            reportedFailure = true;
        }

        private void OnDisable()
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = true;
        }
    }
}
