using Adventure.Player;
using UnityEngine;

namespace Adventure.Obstacles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class HammerHeadDamage : MonoBehaviour
    {
        private void Awake()
        {
            if (!GetComponent<Collider>().isTrigger)
            {
                Debug.LogError("HammerHeadDamage requires a trigger on the hammer head only.", this);
                enabled = false;
            }
        }
        private void OnTriggerEnter(Collider other) { Touch(other); }
        private void OnTriggerStay(Collider other) { Touch(other); }
        private void Touch(Collider other)
        {
            if (!isActiveAndEnabled) return;
            var player = other.GetComponentInParent<PlayerDeath>();
            if (player != null) player.RequestDeath(transform.position);
        }
    }
}