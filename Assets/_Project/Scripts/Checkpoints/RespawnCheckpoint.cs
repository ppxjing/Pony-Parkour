using Adventure.Player;
using UnityEngine;

namespace Adventure.Checkpoints
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RespawnCheckpoint : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;
        [Tooltip("Unit-size visual box. Automatically follows BoxCollider center and size.")]
        [SerializeField] private Transform volumeVisual;
        private BoxCollider volume;
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
        public Quaternion SpawnRotation => Quaternion.Euler(0f, spawnPoint != null ? spawnPoint.eulerAngles.y : transform.eulerAngles.y, 0f);

        private void Awake() { volume = GetComponent<BoxCollider>(); SyncVisual(); }
        private void OnEnable() { SyncVisual(); }
        private void Update() { if (!Application.isPlaying) SyncVisual(); }
        private void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
        private void SyncVisual()
        {
            if (volume == null) volume = GetComponent<BoxCollider>();
            if (volumeVisual == null || volume == null) return;
            // The volume is an editing aid; keep only the trigger active during gameplay.
            bool showVisual = !Application.isPlaying;
            if (volumeVisual.gameObject.activeSelf != showVisual) volumeVisual.gameObject.SetActive(showVisual);
            if (volumeVisual.localPosition != volume.center) volumeVisual.localPosition = volume.center;
            if (volumeVisual.localScale != volume.size) volumeVisual.localScale = volume.size;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            var player = other.GetComponentInParent<PlayerRespawn>();
            if (player != null) player.TryActivateCheckpoint(this);
        }
        private void OnDrawGizmos()
        {
            if (Application.isPlaying) return;
            // Collider handles can repaint Scene view without scheduling an ExecuteAlways Update.
            if (!Application.isPlaying) SyncVisual();
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = previous;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(SpawnPosition, 0.18f);
            Vector3 tip = SpawnPosition + SpawnRotation * Vector3.forward * 0.8f;
            Gizmos.DrawLine(SpawnPosition, tip);
            Gizmos.DrawLine(tip, tip + SpawnRotation * new Vector3(0.15f, 0f, -0.2f));
            Gizmos.DrawLine(tip, tip + SpawnRotation * new Vector3(-0.15f, 0f, -0.2f));
        }
    }
}
