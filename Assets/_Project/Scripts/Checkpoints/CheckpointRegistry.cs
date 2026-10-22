using System.Collections.Generic;
using Adventure.Player;
using UnityEngine;

namespace Adventure.Checkpoints
{
    [DisallowMultipleComponent]
    public sealed class CheckpointRegistry : MonoBehaviour
    {
        [SerializeField] private PlayerRespawn player;
        [Tooltip("Drag checkpoint SCENE instances here. List order does not determine activation order.")]
        [SerializeField] private List<RespawnCheckpoint> checkpoints = new List<RespawnCheckpoint>();
        public PlayerRespawn Player => player;
        public IReadOnlyList<RespawnCheckpoint> Checkpoints => checkpoints;

        private void Awake()
        {
            if (player == null) { Debug.LogError("CheckpointRegistry needs a PlayerRespawn reference.", this); return; }
            player.SetRegistry(this);
        }
        public bool Contains(RespawnCheckpoint checkpoint)
        {
            return isActiveAndEnabled && checkpoint != null && checkpoints.Contains(checkpoint);
        }
    }
}
