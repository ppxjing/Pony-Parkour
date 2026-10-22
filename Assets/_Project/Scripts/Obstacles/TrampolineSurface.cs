using Adventure.Player;
using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TrampolineSurface : MonoBehaviour
    {
        [Tooltip("Vertical rise above the launch point in world metres, using the player's gravity.")]
        [SerializeField, Min(0.1f)] private float bounceHeight = 4f;
        [Tooltip("Inset from the red area's edges in local units. Foot centre must be inside.")]
        [SerializeField, Min(0f)] private float edgeInset = 0.02f;
        private BoxCollider surface;
        public int BounceCount { get; private set; }

        private void Awake() { surface = GetComponent<BoxCollider>(); }
        public bool TryBounce(ThirdPersonMotor player, Vector3 contactNormal)
        {
            if (!isActiveAndEnabled || !surface.enabled || surface.isTrigger) return false;
            // This asset's bed lies in local XY, with its top facing local +Z.
            if (Vector3.Dot(contactNormal, transform.forward) < 0.7f || Vector3.Dot(contactNormal, Vector3.up) < 0.5f)
                return false;
            Vector3 foot = transform.InverseTransformPoint(player.FootPosition) - surface.center;
            Vector3 half = surface.size * 0.5f;
            if (Mathf.Abs(foot.x) > Mathf.Max(0f, half.x - edgeInset) ||
                Mathf.Abs(foot.y) > Mathf.Max(0f, half.y - edgeInset)) return false;
            if (!player.TryBounce(bounceHeight)) return false;
            BounceCount++;
            return true;
        }
        private void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;
            Vector3 size = box.size;
            size.x = Mathf.Max(0f, size.x - 2f * edgeInset);
            size.y = Mathf.Max(0f, size.y - 2f * edgeInset);
            size.z = 0f;
            Gizmos.DrawWireCube(box.center + Vector3.forward * box.size.z * 0.5f, size);
            Gizmos.matrix = previous;
        }
    }
}