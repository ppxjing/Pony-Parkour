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
        private bool initialized;
        private int lastFrameCount = -1;
        public RespawnCheckpoint CurrentCheckpoint { get; private set; }
        public Vector3 SavedPosition => savedPosition;
        public Quaternion SavedRotation => savedRotation;
        public int RespawnCount { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }
        private void OnEnable() { EnsureInitialized(); }
        private void Update()
        {
            EnsureInitialized();
            if (death != null && death.IsDead && Time.time >= death.DiedAt + respawnDelay) RespawnNow();
        }
        public void SetRegistry(CheckpointRegistry source) { registry = source; }
        public bool TryActivateCheckpoint(RespawnCheckpoint checkpoint)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || !death.IsAlive || registry == null ||
                !registry.Contains(checkpoint) || !checkpoint.isActiveAndEnabled) return false;
            CurrentCheckpoint = checkpoint;
            savedPosition = checkpoint.SpawnPosition;
            savedRotation = checkpoint.SpawnRotation;
            return true;
        }
        public void RequestFallRecovery()
        {
            EnsureInitialized();
            if (isActiveAndEnabled && death != null) death.RequestDeath(transform.position);
        }
        public void RespawnNow()
        {
            EnsureInitialized();
            if (!isActiveAndEnabled) return;
            Vector3 position = savedPosition;
            Quaternion rotation = savedRotation;
            if (CurrentCheckpoint == null)
            {
                var race = FindFirstObjectByType<KitchenRaceManager>();
                if (race != null && race.TryGetPlayerRespawnPose(out Vector3 racePosition, out Quaternion raceRotation))
                {
                    position = racePosition;
                    rotation = raceRotation;
                }
            }
            death.Revive(position, rotation, protectionTime);
            RespawnCount++;
        }

        private void EnsureInitialized()
        {
            bool newPlaySession = Application.isPlaying && lastFrameCount >= 0 &&
                                  Time.frameCount < lastFrameCount;
            if (newPlaySession)
            {
                initialized = false;
                registry = null;
                CurrentCheckpoint = null;
                RespawnCount = 0;
            }

            if (death == null) death = GetComponent<PlayerDeath>();
            if (!initialized)
            {
                savedPosition = transform.position;
                savedRotation = transform.rotation;
                initialized = true;
            }

            if (Application.isPlaying && registry == null)
            {
                var registries = FindObjectsByType<CheckpointRegistry>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var candidate in registries)
                {
                    if (candidate != null && candidate.Player == this)
                    {
                        registry = candidate;
                        break;
                    }
                }
            }

            if (Application.isPlaying) lastFrameCount = Time.frameCount;
        }
    }
}
