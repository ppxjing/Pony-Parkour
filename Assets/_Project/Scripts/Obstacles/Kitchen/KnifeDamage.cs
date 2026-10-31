using Adventure.AI;
using Adventure.Player;
using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent]
    public sealed class KnifeDamage : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other) { Touch(other); }
        private void OnTriggerStay(Collider other) { Touch(other); }
        private void Touch(Collider other)
        {
            // Ignore padded AI sensors; only the actual body can receive lethal contact.
            if (!isActiveAndEnabled || other.isTrigger || Time.timeScale <= 0f) return;
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player != null && player.IsAlive) player.RequestDeath(transform.position);
            var ai = other.GetComponentInParent<RacerCollisionBody>();
            if (ai != null) ai.RequestDeath(transform.position);
        }
    }
}
