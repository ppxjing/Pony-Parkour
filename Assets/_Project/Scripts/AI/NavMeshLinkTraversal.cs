using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Adventure.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMeshLinkTraversal : MonoBehaviour
    {
        [Tooltip("Jump apex above the higher endpoint, in metres.")]
        [SerializeField, Min(0.05f)] private float jumpHeight = 1f;
        [Tooltip("Downward acceleration during a jump, in metres per second squared.")]
        [SerializeField, Min(0.1f)] private float gravity = 25f;

        private NavMeshAgent agent;
        private int jumpArea;
        private bool previousAutoTraverse;
        private bool previousUpdatePosition;
        private bool previousUpdateRotation;
        private Vector3 start;
        private Vector3 end;
        private float elapsed;
        private float duration;
        private float verticalSpeed;
        private float jumpGravity;

        public bool IsTraversing { get; private set; }
        public bool IsJumping { get; private set; }
        public int CompletedJumpCount { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            jumpArea = NavMesh.GetAreaFromName("Jump");
        }

        private void OnEnable()
        {
            previousAutoTraverse = agent.autoTraverseOffMeshLink;
            agent.autoTraverseOffMeshLink = false;
        }

        private void Update()
        {
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            {
                CancelTraversal();
                return;
            }

            if (agent.isStopped)
                return;

            if (!IsTraversing)
            {
                if (!agent.isOnOffMeshLink)
                    return;

                OffMeshLinkData data = agent.currentOffMeshLinkData;
                if (!data.valid || !data.activated)
                    return;

                BeginTraversal(data);
            }

            // A destination reset or a removed link cancels the old traversal.
            if (!agent.isOnOffMeshLink || !agent.currentOffMeshLinkData.valid)
            {
                CancelTraversal();
                return;
            }

            elapsed = Mathf.Min(elapsed + Time.deltaTime, duration);
            Vector3 position = Vector3.Lerp(start, end, elapsed / duration);
            if (IsJumping)
                position.y = start.y + verticalSpeed * elapsed - 0.5f * jumpGravity * elapsed * elapsed;
            transform.position = position;

            Vector3 facing = Vector3.ProjectOnPlane(end - start, Vector3.up);
            if (previousUpdateRotation && facing.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(facing), agent.angularSpeed * Time.deltaTime);

            if (elapsed < duration)
                return;

            transform.position = end;
            agent.CompleteOffMeshLink();
            if (IsJumping)
                CompletedJumpCount++;
            RestoreMovement();
        }

        private void BeginTraversal(OffMeshLinkData data)
        {
            Component owner = data.owner as Component;
            NavMeshLink link = owner != null ? owner.GetComponent<NavMeshLink>() : null;
            IsJumping = link != null && link.area == jumpArea;
            start = transform.position;
            end = data.endPos + Vector3.up * agent.baseOffset;
            elapsed = 0f;
            if (IsJumping)
            {
                jumpGravity = Mathf.Max(0.1f, gravity);
                float apex = Mathf.Max(start.y, end.y) + Mathf.Max(0.05f, jumpHeight);
                verticalSpeed = Mathf.Sqrt(2f * jumpGravity * (apex - start.y));
                duration = verticalSpeed / jumpGravity
                    + Mathf.Sqrt(2f * (apex - end.y) / jumpGravity);
            }
            else
            {
                duration = Vector3.Distance(start, end) / Mathf.Max(0.01f, agent.speed);
            }
            duration = Mathf.Max(0.001f, duration);
            previousUpdatePosition = agent.updatePosition;
            previousUpdateRotation = agent.updateRotation;
            agent.updatePosition = false;
            agent.updateRotation = false;
            IsTraversing = true;
        }

        private void RestoreMovement()
        {
            agent.updatePosition = previousUpdatePosition;
            agent.updateRotation = previousUpdateRotation;
            IsTraversing = false;
            IsJumping = false;
        }

        private void CancelTraversal()
        {
            if (!IsTraversing || agent == null)
                return;

            // Return to the takeoff point instead of leaving a pooled/disabled AI in midair.
            transform.position = start;
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.Warp(start);
            RestoreMovement();
        }

        private void OnDisable()
        {
            CancelTraversal();
            if (agent != null)
                agent.autoTraverseOffMeshLink = previousAutoTraverse;
        }
    }
}
