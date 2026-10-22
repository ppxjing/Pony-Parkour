using Adventure.Checkpoints;
using UnityEngine;

namespace Adventure.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerDeath))]
    public sealed class PlayerRespawn : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float respawnDelay = 2f;
        [Tooltip("Seconds of protection from lethal contact after respawn.")]
        [SerializeField, Min(0f)] private float protectionTime = 1f;
        private PlayerDeath death;
        private CheckpointRegistry registry;
        private Vector3 savedPosition;
        private Quaternion savedRotation;
        public RespawnCheckpoint CurrentCheckpoint { get; private set; }
        public Vector3 SavedPosition => savedPosition;
        public Quaternion SavedRotation => savedRotation;
        public int RespawnCount { get; private set; }

        private void Awake()
        {
            death = GetComponent<PlayerDeath>();
            savedPosition = transform.position;
            savedRotation = transform.rotation;
        }
        private void Update()
        {
            if (death.IsDead && Time.time >= death.DiedAt + respawnDelay) RespawnNow();
        }
        public void SetRegistry(CheckpointRegistry source) { registry = source; }
        public bool TryActivateCheckpoint(RespawnCheckpoint checkpoint)
        {
            if (!isActiveAndEnabled || !death.IsAlive || registry == null ||
                !registry.Contains(checkpoint) || !checkpoint.isActiveAndEnabled) return false;
            CurrentCheckpoint = checkpoint;
            savedPosition = checkpoint.SpawnPosition;
            savedRotation = checkpoint.SpawnRotation;
            return true;
        }
        public void RequestFallRecovery()
        {
            if (isActiveAndEnabled) death.RequestDeath(transform.position);
        }
        public void RespawnNow()
        {
            if (!isActiveAndEnabled) return;
            death.Revive(savedPosition, savedRotation, protectionTime);
            RespawnCount++;
        }
    }
}
