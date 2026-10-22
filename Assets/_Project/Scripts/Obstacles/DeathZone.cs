using Adventure.Player;
using UnityEngine;

namespace Adventure.Obstacles
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class DeathZone : MonoBehaviour
    {
        [Tooltip("Editor-only volume preview. Keep the trigger on the root object.")]
        [SerializeField] private Transform volumeVisual;
        private BoxCollider volume;

        private void Awake() { volume = GetComponent<BoxCollider>(); SyncVisual(); }
        private void OnEnable() { SyncVisual(); }
        private void Update() { SyncVisual(); }
        private void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

        private void SyncVisual()
        {
            if (volume == null) volume = GetComponent<BoxCollider>();
            if (volumeVisual == null || volume == null) return;
            // Hide both translucent faces and frame, while the lethal trigger stays active.
            bool show = !Application.IsPlaying(gameObject);
            if (volumeVisual.gameObject.activeSelf != show) volumeVisual.gameObject.SetActive(show);
            if (volumeVisual.localPosition != volume.center) volumeVisual.localPosition = volume.center;
            if (volumeVisual.localScale != volume.size) volumeVisual.localScale = volume.size;
        }
        private void OnTriggerEnter(Collider other) { Touch(other); }
        private void OnTriggerStay(Collider other) { Touch(other); }
        private void Touch(Collider other)
        {
            if (!Application.IsPlaying(gameObject) || !isActiveAndEnabled) return;
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player != null && player.IsAlive) player.RequestDeath(transform.position);
        }
        private void OnDrawGizmos()
        {
            if (Application.IsPlaying(gameObject)) return;
            SyncVisual();
            if (volume == null) return;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(volume.center, volume.size);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}