using Adventure.Player;
using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent]
    public sealed class BananaSlip : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float initialSpeed = 8f;
        [SerializeField, Min(0.05f)] private float duration = 2f;
        [SerializeField] private float spinDegreesPerSecond = 720f;
        [SerializeField, Min(0f)] private float cooldown = 2.5f;
        [Tooltip("Only upward-facing contact counts as stepping on the banana.")]
        [SerializeField, Range(0f, 1f)] private float minimumUpNormal = 0.45f;
        private float nextSlipTime;
        public int SlipCount { get; private set; }
        public bool TrySlip(ThirdPersonMotor player, Vector3 normal)
        {
            if (!isActiveAndEnabled || player == null || Time.time < nextSlipTime ||
                Vector3.Dot(normal, Vector3.up) < minimumUpNormal) return false;
            if (!player.TryStartSlip(initialSpeed, duration, spinDegreesPerSecond)) return false;
            nextSlipTime = Time.time + cooldown;
            SlipCount++;
            return true;
        }
        private void OnDisable() { nextSlipTime = 0f; }
    }
}
